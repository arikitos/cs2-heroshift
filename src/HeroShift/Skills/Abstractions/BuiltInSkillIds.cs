namespace src.SkillsCore.Abstractions;

/*
 * BuiltInSkillIds - the single place every built-in SkillId is declared.
 *
 * One stable entry for every built-in definition, including None. Values keep
 * the established lowercase localization and configuration identifiers.
 *
 * Code must reference these fields (BuiltInSkillIds.Dash, ...) rather than
 * calling SkillId.Create("dash") ad hoc, so a typo is a compile error instead
 * of a silent unknown-skill lookup.
 */
public static class BuiltInSkillIds
{
    public static readonly SkillId None = SkillId.Create("none");
    public static readonly SkillId Aimbot = SkillId.Create("aimbot");
    public static readonly SkillId AimLock = SkillId.Create("aimlock");
    public static readonly SkillId Anomaly = SkillId.Create("anomaly");
    public static readonly SkillId AntyFlash = SkillId.Create("antyflash");
    public static readonly SkillId AntyHead = SkillId.Create("antyhead");
    public static readonly SkillId AreaReaper = SkillId.Create("areareaper");
    public static readonly SkillId Armored = SkillId.Create("armored");
    public static readonly SkillId Assassin = SkillId.Create("assassin");
    public static readonly SkillId Astronaut = SkillId.Create("astronaut");
    public static readonly SkillId Bankrupt = SkillId.Create("bankrupt");
    public static readonly SkillId Baseball = SkillId.Create("baseball");
    public static readonly SkillId Behind = SkillId.Create("behind");
    public static readonly SkillId Berserker = SkillId.Create("berserker");
    public static readonly SkillId BladeMaster = SkillId.Create("blademaster");
    public static readonly SkillId BunnyHop = SkillId.Create("bunnyhop");
    public static readonly SkillId BlastShot = SkillId.Create("blastshot");
    public static readonly SkillId C4Camouflage = SkillId.Create("c4camouflage");
    public static readonly SkillId CarefulBullets = SkillId.Create("carefulbullets");
    public static readonly SkillId Catapult = SkillId.Create("catapult");
    public static readonly SkillId Chameleon = SkillId.Create("chameleon");
    public static readonly SkillId Chicken = SkillId.Create("chicken");
    public static readonly SkillId ChillOut = SkillId.Create("chillout");
    public static readonly SkillId Cutter = SkillId.Create("cutter");
    public static readonly SkillId Cypher = SkillId.Create("cypher");
    public static readonly SkillId Darkness = SkillId.Create("darkness");
    public static readonly SkillId Deactivator = SkillId.Create("deactivator");
    public static readonly SkillId Deaf = SkillId.Create("deaf");
    public static readonly SkillId DeathBomb = SkillId.Create("deathbomb");
    public static readonly SkillId DemonEye = SkillId.Create("demoneye");
    public static readonly SkillId Disarmament = SkillId.Create("disarmament");
    public static readonly SkillId Distancer = SkillId.Create("distancer");
    public static readonly SkillId Dash = SkillId.Create("dash");
    public static readonly SkillId Dracula = SkillId.Create("dracula");
    public static readonly SkillId Duplicator = SkillId.Create("duplicator");
    public static readonly SkillId Dwarf = SkillId.Create("dwarf");
    public static readonly SkillId EnemySpawn = SkillId.Create("enemyspawn");
    public static readonly SkillId ExpensiveAmmo = SkillId.Create("expensiveammo");
    public static readonly SkillId ExplodingBarrel = SkillId.Create("explodingbarrel");
    public static readonly SkillId ExplosiveShot = SkillId.Create("explosiveshot");
    public static readonly SkillId FalconEye = SkillId.Create("falconeye");
    public static readonly SkillId FastReload = SkillId.Create("fastreload");
    public static readonly SkillId FireRain = SkillId.Create("firerain");
    public static readonly SkillId Flash = SkillId.Create("flash");
    public static readonly SkillId Flashlight = SkillId.Create("flashlight");
    public static readonly SkillId Fortnite = SkillId.Create("fortnite");
    public static readonly SkillId FragileBomb = SkillId.Create("fragilebomb");
    public static readonly SkillId FriendlyFire = SkillId.Create("friendlyfire");
    public static readonly SkillId FrozenDecoy = SkillId.Create("frozendecoy");
    public static readonly SkillId Gambler = SkillId.Create("gambler");
    public static readonly SkillId Ghost = SkillId.Create("ghost");
    public static readonly SkillId Giant = SkillId.Create("giant");
    public static readonly SkillId Glaz = SkillId.Create("glaz");
    public static readonly SkillId Glitch = SkillId.Create("glitch");
    public static readonly SkillId Glue = SkillId.Create("glue");
    public static readonly SkillId GodMode = SkillId.Create("godmode");
    public static readonly SkillId Grapple = SkillId.Create("grapple");
    public static readonly SkillId Grenadier = SkillId.Create("grenadier");
    public static readonly SkillId HealingChicken = SkillId.Create("healingchicken");
    public static readonly SkillId HealingSmoke = SkillId.Create("healingsmoke");
    public static readonly SkillId Hermit = SkillId.Create("hermit");
    public static readonly SkillId HolyHandGrenade = SkillId.Create("holyhandgrenade");
    public static readonly SkillId HomingNades = SkillId.Create("homingnades");
    public static readonly SkillId HotBomb = SkillId.Create("hotbomb");
    public static readonly SkillId Iana = SkillId.Create("iana");
    public static readonly SkillId Illiterate = SkillId.Create("illiterate");
    public static readonly SkillId Illusionist = SkillId.Create("illusionist");
    public static readonly SkillId Impostor = SkillId.Create("impostor");
    public static readonly SkillId InfiniteAmmo = SkillId.Create("infiniteammo");
    public static readonly SkillId Inheritance = SkillId.Create("inheritance");
    public static readonly SkillId Jackal = SkillId.Create("jackal");
    public static readonly SkillId Jammer = SkillId.Create("jammer");
    public static readonly SkillId Jester = SkillId.Create("jester");
    public static readonly SkillId JetKick = SkillId.Create("jetkick");
    public static readonly SkillId JumpBan = SkillId.Create("jumpban");
    public static readonly SkillId JumpCurse = SkillId.Create("jumpcurse");
    public static readonly SkillId JumpingJack = SkillId.Create("jumpingjack");
    public static readonly SkillId KillerFlash = SkillId.Create("killerflash");
    public static readonly SkillId Knockback = SkillId.Create("knockback");
    public static readonly SkillId LastGasp = SkillId.Create("lastgasp");
    public static readonly SkillId LifeSwap = SkillId.Create("lifeswap");
    public static readonly SkillId LongKnife = SkillId.Create("longknife");
    public static readonly SkillId LongZeus = SkillId.Create("longzeus");
    public static readonly SkillId MagneticDecoy = SkillId.Create("magneticdecoy");
    public static readonly SkillId Magneto = SkillId.Create("magneto");
    public static readonly SkillId Magnifier = SkillId.Create("magnifier");
    public static readonly SkillId Medic = SkillId.Create("medic");
    public static readonly SkillId Miner = SkillId.Create("miner");
    public static readonly SkillId MoneySwap = SkillId.Create("moneyswap");
    public static readonly SkillId Nightmare = SkillId.Create("nightmare");
    public static readonly SkillId Ninja = SkillId.Create("ninja");
    public static readonly SkillId NoNades = SkillId.Create("nonades");
    public static readonly SkillId NoRecoil = SkillId.Create("norecoil");
    public static readonly SkillId Noclip = SkillId.Create("noclip");
    public static readonly SkillId OneShot = SkillId.Create("oneshot");
    public static readonly SkillId OnlyHead = SkillId.Create("onlyhead");
    public static readonly SkillId PawelJumper = SkillId.Create("paweljumper");
    public static readonly SkillId Phoenix = SkillId.Create("phoenix");
    public static readonly SkillId PsychicDefusing = SkillId.Create("psychicdefusing");
    public static readonly SkillId Pilot = SkillId.Create("pilot");
    public static readonly SkillId Planter = SkillId.Create("planter");
    public static readonly SkillId Poison = SkillId.Create("poison");
    public static readonly SkillId PrimaryBan = SkillId.Create("primaryban");
    public static readonly SkillId Prosthesis = SkillId.Create("prosthesis");
    public static readonly SkillId Push = SkillId.Create("push");
    public static readonly SkillId Pyro = SkillId.Create("pyro");
    public static readonly SkillId QuickShot = SkillId.Create("quickshot");
    public static readonly SkillId RadarHack = SkillId.Create("radarhack");
    public static readonly SkillId Rambo = SkillId.Create("rambo");
    public static readonly SkillId RandomWeapon = SkillId.Create("randomweapon");
    public static readonly SkillId ReZombie = SkillId.Create("rezombie");
    public static readonly SkillId ReactiveArmor = SkillId.Create("reactivearmor");
    public static readonly SkillId Regeneration = SkillId.Create("regeneration");
    public static readonly SkillId Replicator = SkillId.Create("replicator");
    public static readonly SkillId Retreat = SkillId.Create("retreat");
    public static readonly SkillId ReturnToSender = SkillId.Create("returntosender");
    public static readonly SkillId RichBoy = SkillId.Create("richboy");
    public static readonly SkillId Ricochet = SkillId.Create("ricochet");
    public static readonly SkillId RobinHood = SkillId.Create("robinhood");
    public static readonly SkillId Rubber = SkillId.Create("rubber");
    public static readonly SkillId Saper = SkillId.Create("saper");
    public static readonly SkillId SecondLife = SkillId.Create("secondlife");
    public static readonly SkillId Shade = SkillId.Create("shade");
    public static readonly SkillId ShortBomb = SkillId.Create("shortbomb");
    public static readonly SkillId Silent = SkillId.Create("silent");
    public static readonly SkillId Smoker = SkillId.Create("smoker");
    public static readonly SkillId SniperElite = SkillId.Create("sniperelite");
    public static readonly SkillId Soldier = SkillId.Create("soldier");
    public static readonly SkillId SoundMaker = SkillId.Create("soundmaker");
    public static readonly SkillId Spectator = SkillId.Create("spectator");
    public static readonly SkillId SwapPosition = SkillId.Create("swapposition");
    public static readonly SkillId TakeAmmo = SkillId.Create("takeammo");
    public static readonly SkillId TeamTeleport = SkillId.Create("teamteleport");
    public static readonly SkillId Teleporter = SkillId.Create("teleporter");
    public static readonly SkillId Thief = SkillId.Create("thief");
    public static readonly SkillId ThirdEye = SkillId.Create("thirdeye");
    public static readonly SkillId Thorns = SkillId.Create("thorns");
    public static readonly SkillId ThrowingKnife = SkillId.Create("throwingknife");
    public static readonly SkillId ToxicSmoke = SkillId.Create("toxicsmoke");
    public static readonly SkillId Tripwire = SkillId.Create("tripwire");
    public static readonly SkillId Wallhack = SkillId.Create("wallhack");
    public static readonly SkillId Watchmaker = SkillId.Create("watchmaker");
    public static readonly SkillId WeaponsSwap = SkillId.Create("weaponsswap");
    public static readonly SkillId Weightless = SkillId.Create("weightless");
    public static readonly SkillId WildThrow = SkillId.Create("wildthrow");
    public static readonly SkillId Zeus = SkillId.Create("zeus");

    // All 146 built-in IDs, for validation, registration and translation checks.
    public static IReadOnlyList<SkillId> All { get; } =
    [
        None, Aimbot, AimLock, Anomaly, AntyFlash, AntyHead, AreaReaper, Armored, Assassin, Astronaut,
        Bankrupt, Baseball, Behind, Berserker, BladeMaster, BunnyHop, BlastShot, C4Camouflage, CarefulBullets, Catapult, Chameleon,
        Chicken, ChillOut, Cutter, Cypher, Darkness, Deactivator, Deaf, DeathBomb, DemonEye, Disarmament,
        Distancer, Dash, Dracula, Duplicator, Dwarf, EnemySpawn, ExpensiveAmmo, ExplodingBarrel, ExplosiveShot, FalconEye,
        FastReload, FireRain, Flash, Flashlight, Fortnite, FragileBomb, FriendlyFire, FrozenDecoy, Gambler, Ghost,
        Giant, Glaz, Glitch, Glue, GodMode, Grapple, Grenadier, HealingChicken, HealingSmoke, Hermit, HolyHandGrenade,
        HomingNades, HotBomb, Iana, Illiterate, Illusionist, Impostor, InfiniteAmmo, Inheritance, Jackal, Jammer, Jester,
        JetKick, JumpBan, JumpCurse, JumpingJack, KillerFlash, Knockback, LastGasp, LifeSwap, LongKnife, LongZeus,
        MagneticDecoy, Magneto, Magnifier, Medic, Miner, MoneySwap, Nightmare, Ninja, NoNades, NoRecoil,
        Noclip, OneShot, OnlyHead, PawelJumper, Phoenix, PsychicDefusing, Pilot, Planter, Poison, PrimaryBan,
        Prosthesis, Push, Pyro, QuickShot, RadarHack, Rambo, RandomWeapon, ReZombie, ReactiveArmor, Regeneration,
        Replicator, Retreat, ReturnToSender, RichBoy, Ricochet, RobinHood, Rubber, Saper, SecondLife, Shade, ShortBomb,
        Silent, Smoker, SniperElite, Soldier, SoundMaker, Spectator, SwapPosition, TakeAmmo, TeamTeleport, Teleporter,
        Thief, ThirdEye, Thorns, ThrowingKnife, ToxicSmoke, Tripwire, Wallhack, Watchmaker, WeaponsSwap, Weightless,
        WildThrow, Zeus,
    ];
}
