using System;
using ProgEsameUniVPM;
public static class Giocatore
{
    public static string Nome {get; private set;}  = "";

    public static int PuntiVita {get; private set;}
    public static int PuntiVitaMax {get; private set;}

    public static int Stamina {get; private set;}
    public static int StaminaMax {get; private set;}

    public static int Oro {get; private set;}

    /*
    
    */

    public static Stack<Oggetto> Inventario = new();
    public static int InventarioMax {get; private set;} = 10;

    public static void AggiungiOro(int valore)
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
    public static void CambiaPV(int quantita, bool danno)
    {
        PuntiVita += quantita;
        if (PuntiVita < 0)
        {
            //morte
        }else if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
    }
    public static bool CambiaStamina(int quantita)
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

    public static void AggiungiOggettoInventario(Oggetto o)
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
}