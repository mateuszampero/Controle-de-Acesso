namespace ControleAcessoAPI.Models
{
    public class StatusLog
    {
        public int Id { get; set; }
        public string Computador { get; set; }
        public string Usuario { get; set; }
        public string IpAtual { get; set; }
        public bool? EstaNaEmpresa { get; set; }
        public DateTime DataHora { get; set; }

    }
}
