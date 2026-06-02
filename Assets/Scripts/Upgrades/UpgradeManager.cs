using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [System.Serializable]
    public class UpgradeOption
    {
        public string name;
        public string description;
        public Sprite icon;
        public System.Action applyAction;
        public UpgradeRarity rarity;
        public int maxStack = 3;
    }

    public enum UpgradeRarity { Common, Rare, Epic }

    [Header("XP Settings")]
    public float xpToLevelBase = 20f;
    public float xpScalingFactor = 1.4f;

    public float currentXP { get; private set; }
    public float xpToNextLevel { get; private set; }
    public int currentLevel { get; private set; } = 1;

    private List<UpgradeOption> allUpgrades;
    private List<UpgradeOption> chosenUpgrades = new List<UpgradeOption>();

    void Awake()
    {
        Instance = this;
        xpToNextLevel = xpToLevelBase;
        BuildUpgradeList();
    }

    void BuildUpgradeList()
    {
        allUpgrades = new List<UpgradeOption>
        {
            // === OFFENSE ===
            new UpgradeOption
            {
                name = "Damage Up", description = "+25% damage",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.damage = Mathf.RoundToInt(PlayerController.Instance.damage * 1.25f);
                },
                maxStack = 5
            },
            new UpgradeOption
            {
                name = "Attack Speed", description = "+20% fire rate",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.fireRate *= 0.8f;
                },
                maxStack = 5
            },
            new UpgradeOption
            {
                name = "Projectile Size", description = "+30% projectile size",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.projectileSize *= 1.3f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Projectile Speed", description = "+30% projectile speed",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.projectileSpeed *= 1.3f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Double Shot", description = "+1 projectile (spread)",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.projectileCount += 1;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Piercing Shots", description = "Projectiles pierce enemies",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.piercingShots = true;
                },
                maxStack = 1
            },
            new UpgradeOption
            {
                name = "Explosive Rounds", description = "+15% damage (stacks)",
                rarity = UpgradeRarity.Epic,
                applyAction = () => {
                    PlayerController.Instance.damage = Mathf.RoundToInt(PlayerController.Instance.damage * 1.15f);
                },
                maxStack = 2
            },
            new UpgradeOption
            {
                name = "Critical Shot", description = "+10% crit chance (2x dmg)",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.critChance = Mathf.Min(1f, PlayerController.Instance.critChance + 0.1f);
                    PlayerController.Instance.critMultiplier += 0.5f;
                },
                maxStack = 5
            },
            new UpgradeOption
            {
                name = "Chain Lightning", description = "Shock chains to nearby enemies",
                rarity = UpgradeRarity.Epic,
                applyAction = () => {
                    PlayerController.Instance.chainLightning = true;
                },
                maxStack = 1
            },

            // === DEFENSE ===
            new UpgradeOption
            {
                name = "Max Health", description = "+25 max health (heal 50)",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.maxHealth += 25;
                    PlayerController.Instance.Heal(50);
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Health Regen", description = "Regen 2 HP/sec",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.healthRegenRate += 2f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Shield", description = "Absorb 1 hit every 15s",
                rarity = UpgradeRarity.Epic,
                applyAction = () => {
                    PlayerController.Instance.maxHealth += 20;
                    PlayerController.Instance.Heal(20);
                },
                maxStack = 2
            },
            new UpgradeOption
            {
                name = "Vampire", description = "Heal 5 HP on each kill",
                rarity = UpgradeRarity.Epic,
                applyAction = () => {
                    PlayerController.Instance.vampireHealAmount += 5f;
                },
                maxStack = 2
            },
            new UpgradeOption
            {
                name = "Thorns", description = "Reflect 25% damage to attackers",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.thornsDamage += 0.25f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Dodge Mastery", description = "-0.3s dash cooldown",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.dashCooldown = Mathf.Max(0.3f, PlayerController.Instance.dashCooldown - 0.3f);
                },
                maxStack = 3
            },

            // === UTILITY ===
            new UpgradeOption
            {
                name = "Move Speed", description = "+15% movement speed",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.moveSpeed *= 1.15f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "XP Magnet", description = "+50% pickup radius",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    if (XPOrbPool.Instance != null)
                        XPOrbPool.Instance.attractRadius *= 1.5f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Sprint Boost", description = "+25% sprint speed",
                rarity = UpgradeRarity.Common,
                applyAction = () => {
                    PlayerController.Instance.sprintSpeedMultiplier += 0.25f;
                },
                maxStack = 3
            },
            new UpgradeOption
            {
                name = "Frost Aura", description = "Slow nearby enemies by 30%",
                rarity = UpgradeRarity.Epic,
                applyAction = () => {
                    PlayerController.Instance.frostAura = true;
                    PlayerController.Instance.frostAuraRadius += 2f;
                },
                maxStack = 2
            },
            new UpgradeOption
            {
                name = "Dash Impact", description = "Deal damage when dashing through enemies",
                rarity = UpgradeRarity.Rare,
                applyAction = () => {
                    PlayerController.Instance.dashDamage += 15;
                },
                maxStack = 3
            },
        };
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        UIManager.Instance?.UpdateXPBar(currentXP / xpToNextLevel);

        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        xpToNextLevel = Mathf.RoundToInt(xpToLevelBase * Mathf.Pow(xpScalingFactor, currentLevel - 1));

        UIManager.Instance?.UpdateXPBar(currentXP / xpToNextLevel);
        UIManager.Instance?.ShowLevelUpUI(currentLevel);
        AudioManager.Instance?.PlayLevelUp();

        // Pause game for upgrade selection
        Time.timeScale = 0f;
    }

    public List<UpgradeOption> GetUpgradeChoices(int count = 3)
    {
        // Get available upgrades (not maxed out)
        var available = allUpgrades
            .Where(u => chosenUpgrades.Count(c => c.name == u.name) < u.maxStack)
            .ToList();

        // Shuffle
        available = available.OrderBy(x => Random.value).ToList();

        // Weight by rarity (prefer rarer as player levels)
        List<UpgradeOption> weighted = new List<UpgradeOption>();
        foreach (var upgrade in available)
        {
            float weight = 1f;
            if (upgrade.rarity == UpgradeRarity.Rare) weight = 0.5f;
            if (upgrade.rarity == UpgradeRarity.Epic) weight = 0.25f;

            // Increase rare/epic chance with level
            if (currentLevel >= 5 && upgrade.rarity == UpgradeRarity.Rare) weight = 1f;
            if (currentLevel >= 10 && upgrade.rarity == UpgradeRarity.Epic) weight = 0.75f;

            for (int i = 0; i < Mathf.RoundToInt(weight * 100); i++)
                weighted.Add(upgrade);
        }

        if (weighted.Count == 0)
        {
            // Fallback: just use available
            weighted = new List<UpgradeOption>(available);
        }

        return weighted.OrderBy(x => Random.value).Distinct().Take(count).ToList();
    }

    public void ApplyUpgrade(UpgradeOption upgrade)
    {
        chosenUpgrades.Add(upgrade);
        upgrade.applyAction?.Invoke();
        AudioManager.Instance?.PlayUpgrade();

        // Resume game
        Time.timeScale = 1f;
        UIManager.Instance?.HideLevelUpUI();
    }
}
