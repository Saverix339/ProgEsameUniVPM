using System;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProgEsameUniVPM;

public interface IDannegiabile
{
    void Danneggia(int d);
    void Cura(int c);
}

public class Giocatore : IDannegiabile
{
    public string Nome {get; private set;}  = "";

    public int PuntiVita {get; set;}
    public int PuntiVitaMax {get; set;}

    public int Stamina {get; set;}
    public int StaminaMax {get; set;}

    public int Oro {get; set;}

    public Stack<Oggetto> Inventario {get; private set;} = new();
    public int InventarioMax {get; private set;} = 10;

    public Armi? Arma { get; private set; }

    public void EquipaggiaArma(Armi arma) => Arma = arma;

    public List<StatusEffect> StatusEffects { get; } = new();

    public Giocatore(string nome, int pvMax = 20, int staminaMax = 10)
    {
        Nome = nome;
        PuntiVita = pvMax;
        PuntiVitaMax = pvMax;
        Stamina = staminaMax;
        StaminaMax = staminaMax;
        Oro = 0;
    }

    public void InserimentoNome(string s)
    {
        Nome = s;
    }

    public void AggiungiOro(int valore)
    {
        Oro += valore;
        if (Oro < 0)
        {
            Console.WriteLine("Oro minore di 0\n");
            Oro = 0;
            return;
        }
        /*
        if(Oro > 255)
        {
            Oro = 255;
            return;
        }
        */
    }
    public void Danneggia(int danno)
    {
        PuntiVita -= danno;
        UI.MostraDanno(GameManager.Giocatore.Nome, danno);
        if (PuntiVita < 0)
        {
            UI.GameOver(this);
        }
    }
    public void Cura(int cura)
    {
        PuntiVita += cura;
        if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
    }
    /*public void CambiaPV(int quantita, bool danno)
    {
        PuntiVita += quantita;
        if (PuntiVita < 0)
        {
            UI.GameOver(this);
        }else if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
    }*/
    public bool CambiaStamina(int quantita)
    {
        if(-quantita > Stamina)
        {
            Console.WriteLine("Stamina Insufficente!\n");
            //Potremmo usare un evento
            return false;
        }
        Stamina += quantita;
        if (Stamina > StaminaMax)
        {
            Stamina = StaminaMax;
        }
        return true;
    }

    public void AggiungiOggettoInventario(Oggetto o)
    {
        if (o is Consumabili consAggiunto)
        {
            int spazio = 0;
            foreach(var i in Inventario)
            {
                if(i is Consumabili consumabili)
                { 
                    spazio += consumabili.peso;
                }
            }
            spazio += consAggiunto.peso;
            if(spazio > InventarioMax)
            {
                //evento: oggetto non entra nel inventario
                return;
            }
        }
        Inventario.Push(o);
    }

    public Oggetto? RimuoviOggettoInventario()
    {
        if (Inventario.Count() != 0) return Inventario.Pop();
        return null;
    }

    public Dictionary<string, Action<EsplorazioneStanza, Nemico>> AzioniCombattimento = new()
    {
        {
            "attacca", Attacca
        },
        { 
            "abilità", UsaAbilitaArma
        }
    };
    
    public static void Attacca(EsplorazioneStanza contesto, Nemico nem)
    {
        int danno = GameManager.Giocatore.Arma?.potenza ?? 0;
        nem.Danneggia(danno);
    }

    public static void UsaAbilitaArma(EsplorazioneStanza contesto, Nemico nem)
    {
        try{
            Armi armaEquipaggiata = GameManager.Giocatore.Arma ?? throw new NullReferenceException();
            if(armaEquipaggiata.AbiitaArma != null) armaEquipaggiata.AbiitaArma?.Esegui(GameManager.Giocatore, nem);
        }
        catch (NullReferenceException)
        {
            UI.MostraErrore("Nessuna arma.");
        }
    }
    public static void UsaConsumabile(EsplorazioneStanza contesto, Nemico nem)
    {
        if(GameManager.Giocatore.Inventario.Peek() is Consumabili)
        {
            Consumabili consumabile = (Consumabili)GameManager.Giocatore.Inventario.Pop();
            consumabile.Usa();
        }
    }
}

public class StatusEffect
{
    public string Name = "";
    
    public required string target;
    public event EventHandler? onTurnStart;
    //todo

    public static StatusEffect Bruciatura(string target)
    {
        StatusEffect burn = new(){
            target = target,
            Name = "bruciatura"
        };
        if(target == "giocatore")
        {
            burn.onTurnStart += (sender, e) => GameManager.Giocatore.Danneggia(1);
        }
        return burn;
    }
    // status effect bleed
}

public static class JsonSalvataggio
{
    const string percorso = "salvataggio.json";
    private static readonly JsonSerializerOptions Opzioni = new() { WriteIndented = true };

    public static void salva(Giocatore g){
        File.WriteAllText(percorso, JsonSerializer.Serialize(g, Opzioni));
    }
    public static Giocatore? caricaSalvataggio(){
        if(!File.Exists(percorso)){
            UI.MostraErrore("File non trovato.");
            throw new FileNotFoundException();
        }else{
            return JsonSerializer.Deserialize<Giocatore>(File.ReadAllText(percorso));
        }
    }
}

