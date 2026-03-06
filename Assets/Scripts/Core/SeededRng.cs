namespace MiniMapGame.Core
{
    /// <summary>
    /// Deterministic XOR-shift PRNG. Direct port of JSX mkRng().
    /// Same seed always produces the same sequence.
    /// </summary>
    public class SeededRng
    {
        private uint _state;

        public SeededRng(int seed)
        {
            _state = (uint)seed;
            if (_state == 0) _state = 1;
        }

        /// <summary>Returns float in [0, 1)</summary>
        public float Next()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state / (float)0x100000000UL;
        }

        /// <summary>Returns int in [min, max)</summary>
        public int Range(int min, int max)
        {
            return min + (int)(Next() * (max - min));
        }

        /// <summary>Returns float in [min, max)</summary>
        public float Range(float min, float max)
        {
            return min + Next() * (max - min);
        }
    }
}
