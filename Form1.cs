using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Forms1                     
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "http://00.00.00.00/api";
        private List<ComputadorModel> _computadores; 
        private readonly Bitmap _dotGreen = CreateDot(Color.Green);
        private readonly Bitmap _dotRed = CreateDot(Color.Red);

        private static Bitmap CreateDot(Color color)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush br = new SolidBrush(color))
                    g.FillEllipse(br, 0, 0, 15, 15);
            }
            return bmp;
        }

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  
            this.MaximizeBox = false;                        
            this.MinimizeBox = true;

        }
        
        private async Task CarregarComputadores()
        {
            try
            {
                var json = await _httpClient.GetStringAsync($"{BaseUrl}/computadores");
                _computadores = JsonConvert.DeserializeObject<List<ComputadorModel>>(json);

                dataGridView2.Rows.Clear();
                foreach (var c in _computadores)
                {
                   
                    // vindo do BD
                    DateTime raw = c.UltimaVerificacao ?? DateTime.MinValue;

                    // Se Kind ainda for Unspecified, vai tratar como UTC
                    if (raw.Kind == DateTimeKind.Unspecified)
                        raw = DateTime.SpecifyKind(raw, DateTimeKind.Utc);

                    // Converte para horário local do PC
                    DateTime lastLocal = raw == DateTime.MinValue ? raw : raw.ToLocalTime();

                    // 40 segundos para chek se o usuário está online
                    bool online = (DateTime.Now - lastLocal) < TimeSpan.FromSeconds(40);

                  
                    string statusText = !online
                        ? "Desconectado"
                        : (c.EstaNaEmpresa == true ? "Na Empresa" : "Fora da Empresa");

                    dataGridView2.Rows.Add(
                        false,                                
                        online ? _dotGreen : _dotRed,         
                        c.Nome,
                        c.Usuario,
                        c.IpAtual,
                        statusText,                          
                        lastLocal == DateTime.MinValue ? "" : lastLocal.ToString("g"),
                        c.HomeOfficeAutorizado
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar computadores: {ex.Message}");
            }
        }

        private async Task AtualizarPermissoesAsync()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    var nome = row.Cells["Nome"].Value as string;
                    var autorizado = Convert.ToBoolean(row.Cells["HomeOfficeAutorizado"].Value);
                    var comp = _computadores.FirstOrDefault(x => x.Nome == nome);
                    if (comp != null)
                        comp.HomeOfficeAutorizado = autorizado;
                }

                var content = new StringContent(
                    JsonConvert.SerializeObject(_computadores),
                    Encoding.UTF8,
                    "application/json"
                );
                var response = await _httpClient.PostAsync($"{BaseUrl}/atualizar-permissoes", content);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Erro ao atualizar permissões: {response.StatusCode}");
                    return;
                }

                await CarregarComputadores();
                MessageBox.Show("Permissões atualizadas e status recarregado.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }


        public class ComputadorModel
        {
            public string Nome { get; set; }
            public string Usuario { get; set; }
            public string IpAtual { get; set; }
            public bool? EstaNaEmpresa { get; set; }
            public DateTime? UltimaVerificacao { get; set; }
            public bool HomeOfficeAutorizado { get; set; }
        }

        private async void btnaplica_Click(object sender, EventArgs e)
        {
            await AtualizarPermissoesAsync();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            ConfigurarGrid();
            await CarregarComputadores();
        }

        private void ConfigurarGrid()
        {
            dataGridView2.Columns.Clear();
            dataGridView2.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selecionar", HeaderText = "", Width = 30 });

            // ícone de status
            var colIcon = new DataGridViewImageColumn
            {
                Name = "StatusIcon",
                HeaderText = "",
                Width = 17,
                ImageLayout = DataGridViewImageCellLayout.Zoom
            };
            dataGridView2.Columns.Add(colIcon);
            dataGridView2.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome do Dispositivo" });
            dataGridView2.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "Usuário" });
            dataGridView2.Columns.Add(new DataGridViewTextBoxColumn { Name = "IpAtual", HeaderText = "IP Atual" });
            dataGridView2.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" });
            dataGridView2.Columns.Add(new DataGridViewTextBoxColumn { Name = "UltimaVerificacao", HeaderText = "Última Verificação" });
            dataGridView2.Columns.Add(new DataGridViewCheckBoxColumn { Name = "HomeOfficeAutorizado", HeaderText = "Home Office Autorizado", Width = 150 });
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private async void btnlibera_Click(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow == null)
                return;

            dataGridView2.CurrentRow.Cells["HomeOfficeAutorizado"].Value = true;
            await AtualizarPermissoesAsync();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnbanco_Click(object sender, EventArgs e)
        {
            banco bd = new banco();
            bd.ShowDialog();
        }

        private async void button3_Click_1(object sender, EventArgs e)
        {
            await CarregarComputadores(); 
            Cursor.Current = Cursors.Default;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            info inf = new info();
            inf.ShowDialog();
        }
    }
}
