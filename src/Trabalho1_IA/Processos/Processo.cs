namespace ProcessoChat.Processos;

public class Processo
{
    public string tipo { get; set; }
    public string numero { get; set; }
    public string ano { get; set; }
    public string processo { get; set; }
    public string assunto { get; set; }
    public string situacao { get; set; }
    public string data { get; set; }
    public Autor AutorRequerenteDados { get; set; }
}
