namespace MiniMapGame.GameLoop
{
    [System.Serializable]
    public class PlayerStats
    {
        public int maxHP = 100;
        public int currentHP = 100;

        public float HPRatio => maxHP > 0 ? (float)currentHP / maxHP : 0f;
        public bool IsAlive => currentHP > 0;

        public void TakeDamage(int amount)
        {
            currentHP = System.Math.Max(0, currentHP - amount);
        }

        public void Heal(int amount)
        {
            currentHP = System.Math.Min(maxHP, currentHP + amount);
        }

        public void Reset()
        {
            currentHP = maxHP;
        }
    }
}
