using System;

namespace sapra.InfiniteLands{
    public class CompletitionToken{
        public bool complete{get; private set;}
        public Action OnComplete;
        public void Complete(){
            Traveller.DisableTraveller(true);
            if (!complete)
                OnComplete?.Invoke();
            this.complete = true;

        }
    }
}