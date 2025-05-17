using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void AddHealth(int value)
        {
            SetHealth(currentHealth + value);
        }

        public void SetHealth(int value)
        {
            currentHealth = value;

            if (currentHealth > currentMaxHealth)
            {
                currentHealth = currentMaxHealth;
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
            }

            EventBus.Publish(new Actor_OnHealthChanged() { instanceID = GetInstanceID(), currentHealth = currentHealth, maxHealth = currentMaxHealth });
        }

        public void SetStamina(float value)
        {
            currentStamina = value;

            if (currentStamina > currentMaxStamina)
            {
                currentStamina = currentMaxStamina;
            }

            if (currentStamina <= 0)
            {
                currentStamina = 0;
            }

            EventBus.Publish(new Actor_OnStaminaChanged() { instanceID = GetInstanceID(), currentStamina = System.Convert.ToInt32(currentStamina), maxStamina = currentMaxStamina });
        }
    }
}
