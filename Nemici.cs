
using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

public class Nemico : IDannegiabile
{
    public required string Nome {get; set;}
    public string Descrizione = "";
    public int Salute {get; set;}
    public int SaluteMax;

    public List<AbilitaNemico> Abilita = new();
    
    private int totPeso;

    public List<StatusEffect> statusEffects = new();
    
    public void Danneggia(int danno)
    {
        Salute -= danno;
        Logger.Get<Nemico>().LogDebug("{Nemico} subisce {Danno} danni (HP: {HP}/{Max})", Nome, danno, Salute, SaluteMax);
        if(Salute <= 0)
        {
            Logger.Get<Nemico>().LogInformation("{Nemico} sconfitto", Nome);
        }
        UI.MostraDanno(Nome, danno);
    }
    public void Cura(int cura)
    {
        Salute += cura;
        if(Salute > SaluteMax)
        {
            Salute = SaluteMax;
        }
        Logger.Get<Nemico>().LogDebug("{Nemico} curato di {Cura} (HP: {HP}/{Max})", Nome, cura, Salute, SaluteMax);
    }

    public static Nemico Mimic()
    {
        var s = new Nemico()
        {
            Nome = "Mimic",
            Descrizione = "Una creatura mutaforma camuffato come cassa del tesoro, fai attenzione ai suoi denti aguzzi.",
            Salute = 80,
            SaluteMax = 80,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Morso",
                    Danno = 10,
                    PesoProbabilita = 1,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Cura",
                    PesoProbabilita = 1,
                    CondizioneSpeciale = (gioc, nem) => nem.Salute < (nem.SaluteMax /2),
                    EffettiSpeciali = (gioc, nem) => nem.Cura(20)
                }
            }
        };
        s.Pesa();
        return s;
    }

    public static Nemico MaestroArmi()
    {
        var s = new Nemico()
        {
            Nome = "Maestro",
            Descrizione = "Maestro dell'arte della spada.",
            Salute = 160,
            SaluteMax = 160,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Taglio",
                    Danno = 15,
                    PesoProbabilita = 2,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Disarmo",
                    PesoProbabilita = 1,
                    EffettiSpeciali = (gioc, nem) =>
                    {
                        StatusEffect.Indebolimento(Target.Giocatore);
                    }
                }
            }
        };
        s.Pesa();
        return s;
    }

      public static Nemico Guardiano()
    {
        var s = new Nemico()
        {
            Nome = "Guardiano Della Cripta",
            Descrizione = "Maestro dell'arte dello scudo.",
            Salute = 185,
            SaluteMax = 185,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Colpo di Scudo",
                    Danno = 10,
                    PesoProbabilita = 2,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Colpo in Testa",
                    PesoProbabilita = 1,
                    EffettiSpeciali = (gioc, nem) =>
                    {
                        StatusEffect.Indebolimento(Target.Giocatore);
                    }
                }
            }
        };
        s.Pesa();
        return s;
    }


    private void Pesa()
    {
        int tot = 0;
        foreach(var a in Abilita)
        {
            tot += a.PesoProbabilita;
        }
        totPeso = tot;
    }

    public AbilitaNemico ScegliAbilita()
    {
        var rand = new Random();
        int risultato = rand.Next(totPeso +1);
        foreach(var a in Abilita)
        {
            risultato -= a.PesoProbabilita;
            if(risultato<=0) return a;
        }
        throw new Exception();
    }
}

public class AbilitaNemico
{
    public string Nome = "";
    public int Danno = 0;
    public int PesoProbabilita;

    public Func<Giocatore, Nemico, bool>? CondizioneSpeciale;
    public Action<Giocatore, Nemico, int>? AttaccaGiocatore; //Int indica il danno che il giocatore subisce
    public Action<Giocatore, Nemico>? EffettiSpeciali;
    
    public void Esegui(Nemico nemico, Giocatore giocatore)
    {
        if(CondizioneSpeciale != null && CondizioneSpeciale(giocatore, nemico) == false)
        {
            Logger.Get<AbilitaNemico>().LogDebug("Condizione non soddisfatta per {Abilita} di {Nemico}", Nome, nemico.Nome);
            UI.MostraErrore($"Il Nemico non riesce ad usare la abilità {Nome}!!");
            return;
        }
        Logger.Get<AbilitaNemico>().LogDebug("{Nemico} usa {Abilita} (danno: {Danno})", nemico.Nome, Nome, Danno);
        AttaccaGiocatore?.Invoke(giocatore, nemico, Danno);
        EffettiSpeciali?.Invoke(giocatore, nemico);
    }
}

