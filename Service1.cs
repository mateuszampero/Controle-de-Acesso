using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MTZ_SUP.S
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cts;
        private static readonly HttpClient _client = new HttpClient();
        private const string BaseUrl = "http://00.00.00.00/api"; //IP DA API QUE ESTÁ NO SERVIDOR!
        private const string PermissaoFile = @"C:\ProgramData\MTZ_SUP.S\permissao_ho.txt";
        private const string LogoffFlag = @"C:\ProgramData\MTZ_SUP.S\logoff.flag";
        private bool _autorizadoHomeOffice = false;

        public Service1()
        {

            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {

            Directory.CreateDirectory(@"C:\ProgramData\MTZ_SUP.S");
            _cts = new CancellationTokenSource();
            Task.Run(() => LoopStatusAsync(_cts.Token));

        }


        // Novo método assíncrono separado:
        private async Task LoopStatusAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool logoffFlagged = File.Exists(LogoffFlag);
                bool estaNaEmpresa = EstaNaRedeDaEmpresa();
                string usuario = GetActiveUser();
                string ipAtual = GetCurrentIPAddress();

                try
                {
                    // -- BLOQUEIO REPETIDO  ------------------------------------------
                    if (!estaNaEmpresa && logoffFlagged && string.IsNullOrWhiteSpace(usuario))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), token);
                        continue;                                  // nem chega a BD
                    }

                    // -- DENTRO DA EMPRESA  -----------------------------------------
                    if (estaNaEmpresa)
                    {
                        if (logoffFlagged) File.Delete(LogoffFlag);  // limpa flag

                        _autorizadoHomeOffice = await VerificarPermissaoHomeOffice();
                        File.WriteAllText(PermissaoFile, _autorizadoHomeOffice.ToString());
                    }
                    // -- FORA DA EMPRESA  -------------------------------------------
                    else
                    {
                        if (File.Exists(PermissaoFile))
                            bool.TryParse(File.ReadAllText(PermissaoFile), out _autorizadoHomeOffice);
                        else
                            _autorizadoHomeOffice = false;

                        if (!_autorizadoHomeOffice)
                        {
                            File.AppendAllText(PermissaoFile.Replace("permissao_ho.txt", "log.txt"),
                                $"{DateTime.Now:HH:mm:ss} | Vai logoff. IP: {ipAtual}\r\n");

                            ForcarLogoff();
                            File.WriteAllText(LogoffFlag, DateTime.Now.ToString("o")); // cria flag
                            logoffFlagged = true;   // para as próximas passagens
                        }
                    }

                    // **ENVIA STATUS SÓ AQUI, DEPOIS DOS AJUSTES**
                    await EnviarStatusParaAP1(usuario, ipAtual, estaNaEmpresa);
                }
                catch (Exception ex)
                {
                    File.AppendAllText(PermissaoFile.Replace("permissao_ho.txt", "log.txt"),
                        $"{DateTime.Now:HH:mm:ss} | Erro no loop: {ex}\r\n");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), token);
            }
        }

        protected override void OnStop()
        {

            _cts?.Cancel();

        }

        private bool EstaNaRedeDaEmpresa()
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                 .Where(ni => ni.OperationalStatus == OperationalStatus.Up);

            foreach (var ni in adapters)
            {
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var parts = addr.Address.ToString().Split('.');
                        if (parts.Length == 4 && parts[0] == "10" && parts[1] == "67" && parts[2] == "50")
                            return true;
                    }
                }
            }
            return false;
        }

        private string GetCurrentIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "N/A";
        }

        private async Task EnviarStatusParaAP1(string usuario, string ipAtual, bool estaNaEmpresa)
        {
            var payload = new
            {
                Computador = Environment.MachineName,
                Usuario = usuario,
                IpAtual = ipAtual,
                EstaNaEmpresa = estaNaEmpresa,
                DataHora = DateTime.UtcNow   //  <-- em UTC, virá com "Z"
            };
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                await _client.PostAsync($"{BaseUrl}/status", content);
            }
            catch { }
        }

        private async Task<bool> VerificarPermissaoHomeOffice()
        {
            try
            {
                var response = await _client.GetAsync($"{BaseUrl}/verifica-permissao?computador={Environment.MachineName}");
                if (response.IsSuccessStatusCode)
                {
                    return bool.Parse(await response.Content.ReadAsStringAsync());
                }
            }
            catch { }
            return false;
        }

        private string GetActiveUser()
        {
            int sessionId = (int)WTSGetActiveConsoleSessionId();
            if (sessionId == -1) return "DESCONHECIDO";

            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId,
                                           WTS_INFO_CLASS.WTSUserName,
                                           out IntPtr pBuf, out int bytes) && bytes > 1)
            {
                string user = Marshal.PtrToStringAnsi(pBuf);
                WTSFreeMemory(pBuf);
                return string.IsNullOrWhiteSpace(user) ? "DESCONHECIDO" : user;
            }
            return "DESCONHECIDO";
        }

        private enum WTS_INFO_CLASS { WTSUserName = 5 }

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(
            IntPtr hServer, int sessionId, WTS_INFO_CLASS infoClass,
            out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        private void ForcarLogoff()
        {
            try
            {
                int sessionId = (int)WTSGetActiveConsoleSessionId();
                if (sessionId != 0)
                {
                    WTSLogoffSession(IntPtr.Zero, sessionId, false);
                    File.AppendAllText(PermissaoFile.Replace("permissao_ho.txt", "log.txt"),
                        $"{DateTime.Now:HH:mm:ss} | WTSLogoffSession na sessão {sessionId}\r\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(PermissaoFile.Replace("permissao_ho.txt", "log.txt"),
                    $"{DateTime.Now:HH:mm:ss} | Erro no ForcarLogoff: {ex.Message}\r\n");
            }
        }

        // interop
        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);

        public class Payload { }

    }
}
