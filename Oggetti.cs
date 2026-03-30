namespace ProgEsameUniVPM;

//Classe genitore di tutte le sottoclassi inerenti agli ogetti
public class Oggetto
{
    public int Id /*{get; private set;}*/ = 0;
    public string Nome /*{get; private set;}*/ = "Oggetto";

    public string Descrizione = "Oggetto base";

    static int LastId = 0;

    public Oggetto(string Nome, string Descrizione)
    {
        this.Nome = Nome;
        this.Descrizione = Descrizione;
        //In futuro, dovremmo usare un file xml per immagazzinare le descrizioni degli oggetti, credo.
        this.Id = LastId;
        LastId += 1;
    }
}

