namespace ProgEsameUniVPM;

public class Stanza
{
    public string Nome {get;}
    public string Descrizione {get;}

    public int Id {get;}

    private List<Oggetto> _oggettistanza = new();

    public Stanza(string nom, string desc)
    {
        this.Nome = nom;
        this.Descrizione = desc;
    }

    
    //trova oggetto nella stanza con nome
    public Oggetto? TrovaOgg(string nome, bool rimuovi = false)
    {
        foreach (Oggetto ogg in _oggettistanza)
        {
            if(ogg.Nome.Equals(nome))
            {
                return ogg;
            }
        }
        return null;
    }
    //trova oggetto nella stanza con Id
    public Oggetto? TrovaOgg(int Id, bool rimuovi = false)
    {
        foreach (Oggetto ogg in _oggettistanza)
        {
            if(ogg.Id == Id)
            {
                return ogg;
            }
        }
        return null;
    }
}