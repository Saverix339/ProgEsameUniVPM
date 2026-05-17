using System;
using System.Runtime;
using ProgEsameUniVPM;
public class Giocatore
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
    public void CambiaPV(int quantita, bool danno)
    {
        PuntiVita += quantita;
        if (PuntiVita < 0)
        {
            UI.GameOver(this);
        }else if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
    }
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

    public Oggetto RimuoviOggettoInventario()
    {
        return Inventario.Pop();
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
            burn.onTurnStart += (sender, e) => GameManager.Giocatore.CambiaPV(-1, danno: true);
        }
        return burn;
    }
    // status effect bleed
}

public static class JsonSalvataggio
{
    
}

