
using ProgEsameUniVPM;

public class Nemico : IDannegiabile
{
    public string Nome;
    public string Descrizione;
    public int Salute;
    public int SaluteMax;

    public List<StatusEffect> statusEffects = new();
    
    public void Danneggia(int danno)
    {
        Salute -= danno;
        if(Salute <= 0)
        {
            //muore
        }
    }
    public void Cura(int cura)
    {
        Salute += cura;
        if(Salute > SaluteMax)
        {
            Salute = SaluteMax;
        }
    }
}

public class AbilitaNemico
{
    public string Nome = "";
    public int Danno;
    public int PesoProbabilita;

    public Func<Giocatore, Nemico, bool>? CondizioneSpeciale;
    public Action<Giocatore, Nemico, int>? AttaccaGiocatore; //Int indica il danno che il giocatore subisce
    public Action<Giocatore, Nemico>? EffettiSpeciali;
    
    public void Esegui(Nemico nemico, Giocatore giocatore)
    {
        if(CondizioneSpeciale != null && CondizioneSpeciale(giocatore, nemico) == false)
        {
            UI.MostraErrore($"Il Nemico non riesce ad usare la abilità {Nome}!!");
            return;
        }
        AttaccaGiocatore?.Invoke(giocatore, nemico, Danno);
        EffettiSpeciali?.Invoke(giocatore, nemico);
    }
}