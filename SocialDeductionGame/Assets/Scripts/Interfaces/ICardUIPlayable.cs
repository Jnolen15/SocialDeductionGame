public interface ICardUIPlayable
{
    bool CanPlayCardHere(Card cardToPlay);

    void PlayCardHere(int cardID);
}
