namespace ControleAcessoAPI.Models
{
    public class StatusModel
    {
        public string Computador { get; set; }
        public string Usuario { get; set; }
        public string IpAtual { get; set; }
        public bool EstaNaEmpresa { get; set; }
        public DateTime DataHora { get; set; }

    }
}
