namespace ControleAcessoAPI.Models
{
    public class Computador
    {

        public int Id { get; set; }
        public string Nome { get; set; }
        public string Usuario { get; set; }
        public string IpAtual { get; set; }
        public DateTime? UltimaVerificacao { get; set; }
        public bool? EstaNaEmpresa { get; set; }
        public bool HomeOfficeAutorizado { get; set; }

    }
}
