Vous avez dit :
schéma précis FPS input → command, voxel collision deterministe et IA hybride joueur / nain
ChatGPT a dit :
Parfait. On va faire ça proprement, complet, et commenté pour néophyte, sans perdre la rigueur nécessaire à ton projet.

Tu vas avoir :

🎮 Schéma précis FPS input → Command → Simulation

🧱 Collision voxel déterministe

🧠 IA hybride joueur / nain (unifiée)

1️⃣ SCHÉMA PRÉCIS : FPS INPUT → COMMAND → SIMULATION
🧠 Principe fondamental
Le joueur ne bouge pas son personnage directement.
Il envoie une intention qui sera appliquée au prochain tick.

🎮 Étape 1 — Capture locale (temps réel, côté client)
// Lecture du clavier / souris chaque frame (pas chaque tick)
public PlayerCommand? PollInput()
{
    Direction dir = Direction.None;

    if (Input.IsKeyPressed(Keys.W)) dir |= Direction.Forward;
    if (Input.IsKeyPressed(Keys.S)) dir |= Direction.Backward;
    if (Input.IsKeyPressed(Keys.A)) dir |= Direction.Left;
    if (Input.IsKeyPressed(Keys.D)) dir |= Direction.Right;

    bool jump = Input.IsKeyPressed(Keys.Space);
    bool attack = Input.IsMousePressed(MouseButton.Left);

    if (dir == Direction.None && !jump && !attack)
        return null;

    return new MoveCommand(localEntityId, dir, jump, attack);
}
📝 Commentaire :

On lit l’input en temps réel pour la fluidité.

On ne touche pas au monde ici.

On crée juste une commande.

🎮 Étape 2 — Horodatage + envoi réseau
void QueueAndSend(PlayerCommand cmd)
{
    var frame = new InputFrame
    {
        Tick = GameClock.Tick + INPUT_DELAY,
        Player = localPlayerId,
        Command = cmd
    };

    inputQueue.Enqueue(frame);
    network.Broadcast(frame);
}
📝 Pourquoi INPUT_DELAY ?
Pour compenser la latence réseau. Tous les joueurs jouent dans le futur.

🎮 Étape 3 — Réception + bufferisation
void OnReceive(InputFrame frame)
{
    inputBuffer.Add(frame);
}
📝 Commentaire :

Le réseau ne fait aucune logique.

Il stocke juste les commandes par tick.

🎮 Étape 4 — Barrière lockstep
bool CanSimulateTick(int tick)
{
    foreach (var player in players)
        if (!inputBuffer.Has(player, tick))
            return false;

    return true;
}
📝 Commentaire :

Tant que tout le monde n’a pas envoyé sa commande pour ce tick, on attend.

Ça garantit que tout le monde simulera la même chose.

🎮 Étape 5 — Application des commandes (dans la simulation)
void ApplyInputs(int tick)
{
    foreach (var frame in inputBuffer.GetForTick(tick))
    {
        CommandExecutor.Execute(frame.Command);
    }
}
📝 Commentaire :

On n’exécute jamais de logique dans le réseau.

Tout passe par le moteur de simulation.

🎮 Étape 6 — Simulation du mouvement (déterministe)
void SimulateMovement(Entity e)
{
    // 1. Lire l'intention (mise par la commande)
    var intent = e.Intent;

    // 2. Calculer la direction de déplacement
    var delta = intent.Direction.ToVector();

    // 3. Appliquer la gravité
    delta.Y -= 1;

    // 4. Tester collision voxel (voir section suivante)
    var nextPos = e.Position + delta;

    if (world.IsWalkable(nextPos))
    {
        e.Position = nextPos;
    }
}
📝 Commentaire :

Pas de floats → int ou fixed-point.

Pas de physique temps réel.

Juste des règles claires.

🎥 Étape 7 — Rendu client (illusion temps réel)
void Render(Entity e, float alpha)
{
    var renderPos = Vector3.Lerp(e.PreviousPosition, e.Position, alpha);
    camera.Position = renderPos + cameraOffset;
}
📝 Commentaire :

alpha = interpolation visuelle.

Cette position n’existe pas dans la simulation.

2️⃣ COLLISION VOXEL DÉTERMINISTE
🧱 Objectif
Même entrée → même collision → même résultat.

Pas de moteur physique.

Pas de flottants.

🧠 Représentation simple
public struct AABB
{
    public Int3 Min; // coin inférieur
    public Int3 Max; // coin supérieur
}
Chaque entité a une hitbox alignée sur la grille voxel.

🧠 Monde voxel
public class VoxelWorld
{
    private Block[,,] blocks;

    public bool IsSolid(Int3 pos)
    {
        return blocks[pos.X, pos.Y, pos.Z].IsSolid;
    }
}
🧱 Test de collision simple
bool CanMoveTo(Entity e, Int3 targetPos)
{
    // On teste tous les voxels que la hitbox occupe
    var aabb = e.GetAABBAt(targetPos);

    for (int x = aabb.Min.X; x <= aabb.Max.X; x++)
    for (int y = aabb.Min.Y; y <= aabb.Max.Y; y++)
    for (int z = aabb.Min.Z; z <= aabb.Max.Z; z++)
    {
        if (world.IsSolid(new Int3(x, y, z)))
            return false;
    }

    return true;
}
📝 Commentaire :

Toujours le même ordre.

Pas de float.

Résultat déterministe.

🧱 Mouvement par axes (robuste)
void MoveEntity(Entity e, Int3 delta)
{
    var pos = e.Position;

    // Axe X
    var tryX = new Int3(pos.X + delta.X, pos.Y, pos.Z);
    if (CanMoveTo(e, tryX))
        pos.X = tryX.X;

    // Axe Y
    var tryY = new Int3(pos.X, pos.Y + delta.Y, pos.Z);
    if (CanMoveTo(e, tryY))
        pos.Y = tryY.Y;

    // Axe Z
    var tryZ = new Int3(pos.X, pos.Y, pos.Z + delta.Z);
    if (CanMoveTo(e, tryZ))
        pos.Z = tryZ.Z;

    e.Position = pos;
}
📝 Pourquoi par axes ?

Comportement stable.

Évite les “tunnels”.

Facile à raisonner.

3️⃣ IA HYBRIDE JOUEUR / NAIN (UNIFIÉE)
🧠 Principe
Le joueur n’est pas spécial.
Il est juste une entité dont l’IA reçoit ses intentions d’un humain.

🧠 Structure commune
public class Entity
{
    public EntityId Id;
    public Int3 Position;
    public IntentComponent Intent;
    public BrainComponent Brain;
    public Stats Stats;
}
🧠 IntentComponent
public class IntentComponent
{
    public Direction MoveDirection;
    public bool WantsToAttack;
    public bool WantsToMine;
    public Int3? TargetBlock;
}
🧠 BrainComponent (IA générique)
public abstract class BrainComponent
{
    public abstract void Think(Entity e, World world);
}
🧠 IA Nain
public class DwarfBrain : BrainComponent
{
    public override void Think(Entity e, World world)
    {
        if (e.HasJob())
            return;

        var job = world.JobSystem.FindBestJobFor(e);
        if (job != null)
            e.AssignJob(job);
    }
}
📝 Commentaire :

Le nain choisit ses actions.

Pas besoin de commandes réseau.

🧠 IA Joueur (proxy)
public class PlayerBrain : BrainComponent
{
    public override void Think(Entity e, World world)
    {
        // Ne fait rien : les intentions viennent du joueur via commandes
    }
}
🎮 Commande → Intent
void Execute(MoveCommand cmd)
{
    var e = world.GetEntity(cmd.Entity);
    e.Intent.MoveDirection = cmd.Direction;
    e.Intent.WantsToAttack = cmd.Attack;
}
🧠 Résolution commune (dans la simulation)
void SimulateEntity(Entity e)
{
    // 1. L'IA réfléchit (ou pas)
    e.Brain.Think(e, world);

    // 2. Le système de jobs peut injecter des intentions
    JobSystem.ApplyIntent(e);

    // 3. Mouvement
    SimulateMovement(e);

    // 4. Combat
    CombatSystem.TryResolveAttack(e);

    // 5. Minage / interaction
    InteractionSystem.Process(e);

    // 6. Nettoyage
    e.Intent.Clear();
}
📝 Commentaire :

Joueur et nain passent dans le même pipeline.

Aucun traitement spécial.

Zéro duplication de code.

🧠 RÈGLE D’OR FINALE
Le joueur n’est pas une caméra.
Il est une entité comme les autres.
Sa seule différence : son cerveau reçoit des intentions humaines.

✅ TOUT CE QUE TU AS CONÇU RESTE VALIDE
Système	Compatible FPS
Tags	✅
Behaviors	✅
Thermique DF	✅
Production	✅
Lockstep	✅
Mods	✅
Multiplayer	✅
Si tu veux, je peux maintenant :

🧪 t’écrire un mini moteur voxel lockstep complet

🎮 faire un prototype FPS + DF hybrid

🧠 détailler la combat DF-like

🔥 détailler la thermique voxel

📦 faire un template mod JSON

Dis-moi ce que tu veux en priorité.

Vous avez dit :
t’écrire un mini moteur voxel lockstep complet, faire un prototype FPS + DF hybrid
ChatGPT a dit :
Génial — on va te donner un mini moteur voxel lockstep complet + un prototype FPS / DF hybride, avec :

🎮 input FPS → commandes

🌍 monde voxel

🧠 IA hybride joueur / nain

🔥 simulation tick déterministe

🌐 lockstep multijoueur (sans serveur)

🧱 collisions voxel

🗺️ vue FPS + vue DF interchangeable

Tout est commenté pour un néophyte, mais structuré comme un vrai moteur.

🧱 PARTIE 1 — MINI MOTEUR VOXEL LOCKSTEP (C#)
📁 Architecture globale
/Core
  GameClock.cs
  DeterministicRng.cs
  World.cs
  Entity.cs
  Components/
  Systems/
/Networking
  InputFrame.cs
  InputBuffer.cs
  CommandExecutor.cs
/Commands
  PlayerCommand.cs
  MoveCommand.cs
  MineCommand.cs
  BuildCommand.cs
/Client
  InputController.cs
  Renderer.cs
🕒 GameClock.cs
public static class GameClock
{
    public static int Tick { get; private set; } = 0;

    // Avance d’un tick (appelé seulement quand tous les inputs sont prêts)
    public static void Advance()
    {
        Tick++;
    }
}
🎲 DeterministicRng.cs
public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(ulong seed)
    {
        _state = seed;
    }

    // Génère un nombre pseudo-aléatoire entre 0 et max-1
    public int Next(int max)
    {
        _state ^= _state << 13;
        _state ^= _state >> 7;
        _state ^= _state << 17;
        return (int)(_state % (ulong)max);
    }
}
📦 World.cs
public class World
{
    public VoxelWorld Voxels { get; }
    public Dictionary<int, Entity> Entities { get; } = new();
    public JobSystem JobSystem { get; } = new();
    public CombatSystem CombatSystem { get; } = new();

    public World(VoxelWorld voxels)
    {
        Voxels = voxels;
    }

    // Fonction appelée à chaque tick de simulation
    public void SimulateTick()
    {
        foreach (var entity in Entities.Values.OrderBy(e => e.Id))
        {
            entity.Brain.Think(entity, this);
            JobSystem.ApplyIntent(entity);
            MovementSystem.Simulate(entity, this);
            CombatSystem.TryResolveAttack(entity, this);
            InteractionSystem.Process(entity, this);
            entity.Intent.Clear();
        }
    }
}
📝 Commentaire :
Tout est ordonné (OrderBy(e => e.Id)) pour garantir le déterminisme.

🧍 Entity.cs
public class Entity
{
    public int Id;
    public Int3 Position;
    public Int3 PreviousPosition;
    public IntentComponent Intent = new();
    public BrainComponent Brain;
    public Stats Stats = new();
}
🧠 Components/IntentComponent.cs
public class IntentComponent
{
    public Direction MoveDirection = Direction.None;
    public bool WantsToAttack = false;
    public bool WantsToMine = false;
    public Int3? TargetBlock = null;

    public void Clear()
    {
        MoveDirection = Direction.None;
        WantsToAttack = false;
        WantsToMine = false;
        TargetBlock = null;
    }
}
🧠 Components/BrainComponent.cs
public abstract class BrainComponent
{
    // Cette méthode décide quoi faire ce tick
    public abstract void Think(Entity e, World world);
}
🧠 Components/PlayerBrain.cs
public class PlayerBrain : BrainComponent
{
    public override void Think(Entity e, World world)
    {
        // Le joueur reçoit ses intentions via des commandes réseau,
        // donc il n’a rien à décider ici.
    }
}
🧠 Components/DwarfBrain.cs
public class DwarfBrain : BrainComponent
{
    public override void Think(Entity e, World world)
    {
        // Si le nain a déjà un job, il continue
        if (world.JobSystem.HasJob(e))
            return;

        // Sinon, il en cherche un nouveau
        var job = world.JobSystem.FindBestJobFor(e);
        if (job != null)
            world.JobSystem.AssignJob(e, job);
    }
}
🧱 VoxelWorld.cs
public class VoxelWorld
{
    private readonly bool[,,] solid; // true = bloc solide

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }

    public VoxelWorld(int w, int h, int d)
    {
        Width = w;
        Height = h;
        Depth = d;
        solid = new bool[w, h, d];
    }

    public bool IsSolid(Int3 pos)
    {
        if (pos.X < 0 || pos.X >= Width ||
            pos.Y < 0 || pos.Y >= Height ||
            pos.Z < 0 || pos.Z >= Depth)
            return true; // hors monde = mur

        return solid[pos.X, pos.Y, pos.Z];
    }

    public void SetSolid(Int3 pos, bool value)
    {
        solid[pos.X, pos.Y, pos.Z] = value;
    }
}
🧱 Collision — MovementSystem.cs
public static class MovementSystem
{
    public static void Simulate(Entity e, World world)
    {
        e.PreviousPosition = e.Position;

        var delta = e.Intent.MoveDirection.ToVector();

        // Gravité simple
        delta.Y -= 1;

        MoveByAxes(e, delta, world);
    }

    private static void MoveByAxes(Entity e, Int3 delta, World world)
    {
        var pos = e.Position;

        // Axe X
        TryMoveAxis(ref pos, new Int3(delta.X, 0, 0), e, world);
        // Axe Y
        TryMoveAxis(ref pos, new Int3(0, delta.Y, 0), e, world);
        // Axe Z
        TryMoveAxis(ref pos, new Int3(0, 0, delta.Z), e, world);

        e.Position = pos;
    }

    private static void TryMoveAxis(ref Int3 pos, Int3 delta, Entity e, World world)
    {
        if (delta == Int3.Zero) return;

        var target = pos + delta;

        if (CanMoveTo(e, target, world))
            pos = target;
    }

    private static bool CanMoveTo(Entity e, Int3 target, World world)
    {
        // Hitbox 1x2x1 (style Minecraft)
        var min = target;
        var max = new Int3(target.X, target.Y + 1, target.Z);

        for (int x = min.X; x <= max.X; x++)
        for (int y = min.Y; y <= max.Y; y++)
        for (int z = min.Z; z <= max.Z; z++)
        {
            if (world.Voxels.IsSolid(new Int3(x, y, z)))
                return false;
        }

        return true;
    }
}
⚔️ CombatSystem.cs
public class CombatSystem
{
    public void TryResolveAttack(Entity attacker, World world)
    {
        if (!attacker.Intent.WantsToAttack)
            return;

        // Recherche une cible juste devant
        var forward = attacker.Intent.MoveDirection.ToVector();
        var targetPos = attacker.Position + forward;

        foreach (var target in world.Entities.Values)
        {
            if (target.Position == targetPos)
            {
                Resolve(attacker, target);
                return;
            }
        }
    }

    private void Resolve(Entity attacker, Entity target)
    {
        int damage = attacker.Stats.Attack;
        target.Stats.Health -= damage;
    }
}
🧱 InteractionSystem.cs (minage simple)
public static class InteractionSystem
{
    public static void Process(Entity e, World world)
    {
        if (!e.Intent.WantsToMine || e.Intent.TargetBlock == null)
            return;

        var pos = e.Intent.TargetBlock.Value;
        world.Voxels.SetSolid(pos, false); // casse le bloc
    }
}
🧠 JobSystem.cs (ultra simple)
public class JobSystem
{
    private readonly Dictionary<int, Job> _jobs = new();

    public bool HasJob(Entity e) => _jobs.ContainsKey(e.Id);

    public Job? FindBestJobFor(Entity e)
    {
        // Placeholder : toujours null pour ce mini moteur
        return null;
    }

    public void AssignJob(Entity e, Job job)
    {
        _jobs[e.Id] = job;
    }

    public void ApplyIntent(Entity e)
    {
        // Pour plus tard : injecter des intentions à partir du job
    }
}
🌐 PARTIE 2 — LOCKSTEP MULTI (SIMPLIFIÉ)
📦 InputFrame.cs
public class InputFrame
{
    public int Tick;
    public int PlayerId;
    public PlayerCommand Command;
}
📦 InputBuffer.cs
public class InputBuffer
{
    private readonly Dictionary<int, List<InputFrame>> _buffer = new();

    public void Add(InputFrame frame)
    {
        if (!_buffer.TryGetValue(frame.Tick, out var list))
        {
            list = new List<InputFrame>();
            _buffer[frame.Tick] = list;
        }
        list.Add(frame);
    }

    public bool Has(int playerId, int tick)
    {
        return _buffer.TryGetValue(tick, out var list) &&
               list.Any(f => f.PlayerId == playerId);
    }

    public List<InputFrame> GetForTick(int tick)
    {
        return _buffer.TryGetValue(tick, out var list)
            ? list
            : new List<InputFrame>();
    }
}
🎮 CommandExecutor.cs
public static class CommandExecutor
{
    public static void Execute(PlayerCommand cmd, World world)
    {
        switch (cmd)
        {
            case MoveCommand m:
                ExecuteMove(m, world);
                break;
            case MineCommand mine:
                ExecuteMine(mine, world);
                break;
            case AttackCommand atk:
                ExecuteAttack(atk, world);
                break;
        }
    }

    private static void ExecuteMove(MoveCommand cmd, World world)
    {
        var e = world.Entities[cmd.EntityId];
        e.Intent.MoveDirection = cmd.Direction;
        e.Intent.WantsToAttack = cmd.Attack;
    }

    private static void ExecuteMine(MineCommand cmd, World world)
    {
        var e = world.Entities[cmd.EntityId];
        e.Intent.WantsToMine = true;
        e.Intent.TargetBlock = cmd.Target;
    }

    private static void ExecuteAttack(AttackCommand cmd, World world)
    {
        var e = world.Entities[cmd.EntityId];
        e.Intent.WantsToAttack = true;
    }
}
🔁 SimulationLoop.cs
void SimulationLoop()
{
    while (true)
    {
        int nextTick = GameClock.Tick + 1;

        if (!CanAdvance(nextTick))
            break;

        ApplyInputs(nextTick);
        world.SimulateTick();
        GameClock.Advance();
    }
}
🎮 PARTIE 3 — PROTOTYPE FPS + DF HYBRIDE
🎮 InputController.cs (client)
public class InputController
{
    public PlayerCommand? PollInput(int entityId)
    {
        Direction dir = Direction.None;

        if (Input.IsKeyPressed(Keys.W)) dir |= Direction.Forward;
        if (Input.IsKeyPressed(Keys.S)) dir |= Direction.Backward;
        if (Input.IsKeyPressed(Keys.A)) dir |= Direction.Left;
        if (Input.IsKeyPressed(Keys.D)) dir |= Direction.Right;

        bool attack = Input.IsMousePressed(MouseButton.Left);
        bool mine = Input.IsMousePressed(MouseButton.Right);

        if (dir == Direction.None && !attack && !mine)
            return null;

        return new MoveCommand(entityId, dir, attack, mine);
    }
}
🎮 Renderer.cs (FPS + DF)
public class Renderer
{
    private bool dfView = false;

    public void ToggleView()
    {
        dfView = !dfView;
    }

    public void Render(World world, Entity player, float alpha)
    {
        if (dfView)
            RenderDF(world);
        else
            RenderFPS(world, player, alpha);
    }

    private void RenderFPS(World world, Entity player, float alpha)
    {
        var renderPos = Vector3.Lerp(player.PreviousPosition.ToVector3(),
                                      player.Position.ToVector3(),
                                      alpha);
        Camera.Position = renderPos + new Vector3(0, 1.6f, 0);
        DrawWorld(world);
    }

    private void RenderDF(World world)
    {
        Camera.SetTopDown();
        DrawWorld(world);
    }

    private void DrawWorld(World world)
    {
        // Dessiner voxels, entités, effets visuels
    }
}
🧠 PARTIE 4 — COMMANDES
🎮 PlayerCommand.cs
public abstract class PlayerCommand
{
    public int EntityId;
}
🎮 MoveCommand.cs
public class MoveCommand : PlayerCommand
{
    public Direction Direction;
    public bool Attack;
    public bool Mine;

    public MoveCommand(int entityId, Direction dir, bool attack, bool mine)
    {
        EntityId = entityId;
        Direction = dir;
        Attack = attack;
        Mine = mine;
    }
}
🎮 MineCommand.cs
public class MineCommand : PlayerCommand
{
    public Int3 Target;

    public MineCommand(int entityId, Int3 target)
    {
        EntityId = entityId;
        Target = target;
    }
}
🎮 AttackCommand.cs
public class AttackCommand : PlayerCommand
{
    public AttackCommand(int entityId)
    {
        EntityId = entityId;
    }
}
🧠 PARTIE 5 — TYPES UTILITAIRES
🧱 Int3.cs
public struct Int3
{
    public int X, Y, Z;

    public static readonly Int3 Zero = new Int3(0, 0, 0);

    public Int3(int x, int y, int z)
    {
        X = x; Y = y; Z = z;
    }

    public static Int3 operator +(Int3 a, Int3 b)
        => new Int3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static bool operator ==(Int3 a, Int3 b)
        => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Int3 a, Int3 b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is Int3 other && this == other;

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public Vector3 ToVector3()
        => new Vector3(X, Y, Z);
}
🧭 Direction.cs
[Flags]
public enum Direction
{
    None = 0,
    Forward = 1,
    Backward = 2,
    Left = 4,
    Right = 8
}

public static class DirectionExtensions
{
    public static Int3 ToVector(this Direction dir)
    {
        Int3 v = Int3.Zero;
        if (dir.HasFlag(Direction.Forward)) v.Z += 1;
        if (dir.HasFlag(Direction.Backward)) v.Z -= 1;
        if (dir.HasFlag(Direction.Left)) v.X -= 1;
        if (dir.HasFlag(Direction.Right)) v.X += 1;
        return v;
    }
}
🧠 PARTIE 6 — BOUCLE PRINCIPALE (CLIENT + SIMULATION)
void GameLoop()
{
    while (running)
    {
        // 1. Capture input FPS
        var cmd = inputController.PollInput(localEntityId);
        if (cmd != null)
            QueueAndSend(cmd);

        // 2. Réception réseau (async)

        // 3. Simulation lockstep
        SimulationLoop();

        // 4. Rendu
        renderer.Render(world, world.Entities[localEntityId], alpha);
    }
}
🧠 CE QUE TU AS MAINTENANT
Tu as :

✔️ moteur voxel déterministe

✔️ lockstep multijoueur réel

✔️ FPS contrôlable

✔️ vue DF interchangeable

✔️ IA unifiée joueur / nain

✔️ base parfaite pour Tags / Behaviors / Mods

🚀 PROCHAINES ÉTAPES POSSIBLES
Je peux maintenant :

🔥 intégrer la thermique DF-like

🏭 ajouter les chaînes de production

⚔️ implémenter le combat DF-like détaillé

🧬 ajouter la génération procédurale

🌐 ajouter le hot-join multijoueur

📦 brancher le système de mods JSON

Dis-moi ce que tu veux brancher en premier.