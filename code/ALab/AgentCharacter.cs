using System.Collections.Generic;
using System;
using System.Linq;
using Godot;
using System.Diagnostics;
using Rehab.Agents.Character;
namespace Rehab;

public partial class AgentCharacter : Agent
{
    public enum CharacterType
    {
        Crash = 0,
        Cortex = 1,
        Coco = 2, // Unused
        Nina = 3,
        EvilCrash = 4, // DummyFrontendCharacter
        MechaBandicoot = 5,
    }

    public virtual CharacterType CharType => CharacterType.Crash;
    public Vector3 char_velocity = Vector3.Zero;
    PlayerCamera physCam;
    public bool isReparenting;
    public bool isSwitchingChunks;
    float headdirX;
    float headdirY;
    float footsteptimer;
    bool footsteplast;
    bool gravityOn = true;
    float currentSpeed;
    public float spinTimer;
    bool isCrouched;
    bool isJumping = false;
    float pushTimer;
    bool groundedByPush;
    const float PushAnimGrace = 0.2f;
    const float PushFollowDistance = 2.0f;
    Node3D lastPushedBody;
    public float slideTimer;
    Vector3 slideDir;
    bool slideJumpBuffered;
    bool isSlideJumping;
    float slideJumpLandTimer;
    public float coyoteTimer;
    public bool BlockMovement;
    public Vector3 SpawnPos;
    public Vector3 SpawnRot;
    CollisionShape3D DynamicCol;
    //MeshInstance3D DynamicColVis;

    AudioStream FS_Dirt_1;
    AudioStream FS_Dirt_2;
    AudioStream FS_Grass_1;
    AudioStream FS_Grass_2;
    AudioStream FS_Metal_1;
    AudioStream FS_Metal_2;
    AudioStream FS_Sand_1;
    AudioStream FS_Sand_2;
    AudioStream FS_Stone_1;
    AudioStream FS_Stone_2;
    AudioStream FS_Water_1;
    AudioStream FS_Water_2;
    AudioStream FS_Wood_1;
    AudioStream FS_Wood_2;
    AudioStream FS_Tile_1;
    AudioStream FS_Tile_2;
    AudioStream FS_Slippy;

    public const float AirGravity = 50f;
    public virtual float WalkSpeed => 2.5f;
    public virtual float RunSpeed => 9.0f;
    public virtual float SpinLength => 0.4f;
    public virtual float CrawlSpeed => 1.75f;
    public virtual float SlideSpeed => 18.0f;
    public virtual float SlideTime => 0.4f;
    public virtual float SlideJumpSpeed => 14.0f;
    public virtual float JumpSpeed => 15.0f;
    public virtual float PushMass => 80.0f;
    public virtual float PushStrength => 0.2f;
    public virtual float SpinHitRadius => 1.25f;
    public virtual float SpinHitSpeed => 16.0f;
    public virtual float SpinHitLift => 7.0f;
    public virtual float StepHeight => 1.37f;
    HashSet<Node3D> spinHitBodies = new();
    SphereShape3D spinHitShape;

    bool infiniteJumps = false;
    protected virtual int TimesCanJump => 2;
    int timesJumped = 0;

    public static AgentCharacter activeCharacter;
    public static Dictionary<int, string> ActiveActorTypes = new();

    public override void _Ready()
    {
        base._Ready();

        if (!ActiveActorTypes.ContainsKey((int)CharType))
        {
            ActiveActorTypes[(int)CharType] = GetPath();
        }
        else
        {
            Visible = false;
            ProcessMode = ProcessModeEnum.Disabled;
        }

        CreateShadow(0, Vector2.One, 0);

        DynamicCol = new CollisionShape3D();
        //var box = new BoxShape3D();
        var box = new ConvexPolygonShape3D();
        box.Points = [
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 1, 0.5f),
            new Vector3(-0.5f, 1, -0.5f),
            new Vector3(0.5f, 1, -0.5f),
            new Vector3(-0.5f, 1, 0.5f),
        ];
        DynamicCol.Shape = box;
        DynamicCol.Name = "DynamicCol";
        AddChild(DynamicCol);
        //DynamicColVis = new MeshInstance3D();
        //var boxmesh = new BoxMesh();
        //DynamicColVis.Mesh = boxmesh;
        //AddChild(DynamicColVis);
        Set("floor_constant_speed", true);

        SpawnPos = GlobalPosition;
        SpawnRot = GlobalRotationDegrees;
        if (activeCharacter != null)
        {
            return;
        }
        if (!ParentScene.ActiveScene)
        {
            return;
        }

        activeCharacter = this;
        physCam = RehabScene.PlayerCam;
        physCam.SetupCam(this);

        FS_Dirt_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_dirt_3.sample");
        FS_Dirt_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_dirt_5.sample");
        FS_Grass_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_grass_2.sample");
        FS_Grass_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_grass_3.sample");
        FS_Metal_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_metal_1.sample");
        FS_Metal_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_metal_5.sample");
        FS_Sand_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_sand_1.sample");
        FS_Sand_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_sand_3.sample");
        FS_Stone_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_stone_3.sample");
        FS_Stone_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_stone_5.sample");
        FS_Water_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/FS_WAT1.sample");
        FS_Water_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/FS_WAT2.sample");
        FS_Wood_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_wood_1.sample");
        FS_Wood_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/fs_wood_2.sample");
        FS_Tile_1 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/L09_Cortex_boots_tile_2.sample");
        FS_Tile_2 = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/L09_Cortex_boots_tile_7.sample");
        FS_Slippy = (AudioStream)ResourceLoader.Load(RehabGame.AssetsPath + "Sounds/Surface/L03_Tribesman_fs.sample");
        RehabGame.SetCheckPoint(SpawnPos, SpawnRot, ParentScene.Name, true);

        Character_ActivePostSetup();
    }

    public virtual void Character_ActivePostSetup()
    {
        if (RehabScene.Root.XR_Enabled)
        {
            XR_Setup();
        }
    }

    public override void _ExitTree()
    {
        if (isReparenting)
        {
            isReparenting = false;
            return;
        }
        if (ActiveActorTypes.ContainsKey((int)CharType))
        {
            if (ActiveActorTypes[(int)CharType] == GetPath())
            {
                ActiveActorTypes.Remove((int)CharType);
            }
        }
        if (activeCharacter == this)
        {
            activeCharacter = null;
        }
    }

    public override void _PhysicsProcess(double delt)
    {
        ActiveActorTypes[(int)CharType] = GetPath();
        if (ProcessMode == ProcessModeEnum.Disabled) return;
        float delta = (float)delt;
        UpdateDynamicCollision(delta);
        if (activeCharacter != this)
        {
            UpdateNPC(delta);
            return;
        }

        UpdateMovement(delta);
        UpdateHeadAnim(delta);
        UpdateFootStep(delta);
        if (RehabScene.Root.XR_Enabled)
        {
            RehabScene.Root.XR_Origin.GlobalPosition = GlobalPosition + (Vector3.Up * RehabGame.XR_Height);
        }
    }

    void UpdateMovement(float delta)
    {
        if (BlockMovement) return;
        Vector3 direction = Vector3.Zero;
        bool onFloor = (bool)Call("is_on_floor") || groundedByPush;
        groundedByPush = false;

        direction.X -= Input.GetActionStrength(RehabGame.Pad_Dpad_Up);
        direction.X += Input.GetActionStrength(RehabGame.Pad_Dpad_Down);
        if (direction.X == 0)
        {
            direction.X -= Input.GetActionStrength(RehabGame.Pad_LStick_Up);
            direction.X += Input.GetActionStrength(RehabGame.Pad_LStick_Down);
        }
        direction.Z += Input.GetActionStrength(RehabGame.Pad_Dpad_Right);
        direction.Z -= Input.GetActionStrength(RehabGame.Pad_Dpad_Left);
        if (direction.Z == 0)
        {
            direction.Z += Input.GetActionStrength(RehabGame.Pad_LStick_Right);
            direction.Z -= Input.GetActionStrength(RehabGame.Pad_LStick_Left);
        }

        direction = direction.Clamp(-Vector3.One, Vector3.One);
        if (RehabScene.Root.XR_Enabled)
        {
            var camvector = RehabScene.Root.XR_Origin.XR_Camera.GlobalTransform.Basis.Z;
            camvector.Y = 0f;
            camvector = camvector.Normalized();
            var camright = new Vector3(camvector.Z, 0, -camvector.X);
            direction = (direction.X * camvector) + (direction.Z * camright);
        }
        else
        {
            direction = (direction.X * physCam.camvector) + (direction.Z * physCam.camright);
        }
        direction.Y = 0f;
        float dirLength = Math.Abs(direction.Length());
        var pressed = dirLength > 0.05;

        if (pushTimer > 0f)
        {
            pushTimer -= delta;
        }
        if (slideJumpLandTimer > 0f)
        {
            slideJumpLandTimer -= delta;
        }
        if (lastPushedBody != null)
        {
            if (!IsInstanceValid(lastPushedBody) || pushTimer <= 0f)
            {
                lastPushedBody = null;
            }
            else
            {
                var toBody = lastPushedBody.GlobalPosition - GlobalPosition;
                toBody.Y = 0f;
                var moveDir = new Vector3(char_velocity.X, 0f, char_velocity.Z);
                if (toBody.Length() < PushFollowDistance && moveDir.LengthSquared() > 0.01f && toBody.Normalized().Dot(moveDir.Normalized()) > 0.5f)
                {
                    pushTimer = PushAnimGrace;
                }
            }
        }
        if (spinTimer > 0f)
        {
            spinTimer -= delta;
            if (spinTimer <= 0f)
            {
                Call("set_collision_mask_value", 9, true);
                Call("set_collision_mask_value", 10, true);
                Call("set_collision_mask_value", 11, true);
                Call("set_collision_mask_value", 12, true);
                Call("set_collision_mask_value", 13, true);
                Call("set_collision_mask_value", 14, true);
            }
        }
        if (slideTimer > 0f)
        {
            slideTimer -= delta;
            if (slideTimer <= 0f)
            {
                EndSlide();
            }
        }

        if (Input.IsActionJustPressed(RehabGame.Pad_Select))
        {
            infiniteJumps = !infiniteJumps;
            RehabGame.DisplayMessage("INFINITE JUMPS " + (infiniteJumps == true ? Tr("#FE-On") : Tr("#FE-Off")));
        }

        bool jumpPressed = Input.IsActionJustPressed(RehabGame.Pad_Cross);
        if (jumpPressed && slideTimer > SlideTime * 0.5f)
        {
            slideJumpBuffered = true;
            jumpPressed = false;
        }
        if (slideJumpBuffered && slideTimer > 0f && slideTimer <= SlideTime * 0.5f)
        {
            slideJumpBuffered = false;
            jumpPressed = true;
        }

        if (jumpPressed && !isSlideJumping)
        {
            if (timesJumped < TimesCanJump || infiniteJumps == true)
            {
                isJumping = true;
                timesJumped++;

                char_velocity.Y = JumpSpeed;
                
                if (slideTimer > 0f)
                {
                    EndSlide();
                    isSlideJumping = true;
                    DoAnimation(Crash.Anim.SlideJump, false);
                    DoSound(0, 1f, 0f);
                }
                else if ((ActiveAnim != Crash.Anim.JumpStart || ActiveAnim != Crash.Anim.DoubleJumpFistUp) && spinTimer <= 0.0)
                {
                    DoAnimation(timesJumped == 1 ? Crash.Anim.JumpStart : Crash.Anim.DoubleJumpFistUp, false);
                    DoSound(0, 1f, 0f);
                }
            }
        }
        if (Input.IsActionJustPressed(RehabGame.Pad_Triangle))
        {
            RehabGame.DisplayHUD();
        }
        if (Input.IsActionJustPressed(RehabGame.Pad_R1))
        {
            char_velocity.Y = 0f;
            gravityOn = !gravityOn;
            if (gravityOn)
            {
                RehabGame.DisplayMessage("GRAVITY " + Tr("#FE-On"));
            }
            else
            {
                RehabGame.DisplayMessage("GRAVITY " + Tr("#FE-Off"));
            }
        }
        if (Input.IsActionJustPressed(RehabGame.Pad_Square) && spinTimer <= 0f && slideTimer <= 0f && !isSlideJumping && SpinLength > 0f && !isCrouched)
        {
            spinTimer = SpinLength;
            spinHitBodies.Clear();
            DoAnimation(Crash.Anim.Spin, true);
            if (System.Random.Shared.Next(0, 2) == 0)
            {
                DoSound(2, 1f, 0f);
            }
            else
            {
                DoSound(3, 1f, 0f);
            }
            Call("set_collision_mask_value", 12, true);
            Call("set_collision_mask_value", 9, false);
            Call("set_collision_mask_value", 10, false);
            Call("set_collision_mask_value", 11, false);
            Call("set_collision_mask_value", 13, false);
            Call("set_collision_mask_value", 14, false);
        }
        if (Input.IsActionJustPressed(RehabGame.Pad_Circle))
        {
            if (pressed && slideTimer <= 0f && onFloor && SlideSpeed > 0f)
            {
                slideTimer = SlideTime;
                slideDir = direction.Normalized();
                DoAnimation(Crash.Anim.SlidePose, true);
                DoSound(6, 1f, 0f);
                Call("set_collision_mask_value", 11, true);
                Call("set_collision_mask_value", 9, false);
                Call("set_collision_mask_value", 10, false);
                Call("set_collision_mask_value", 12, false);
                Call("set_collision_mask_value", 13, false);
                Call("set_collision_mask_value", 14, false);
            }
        }
        if (Input.IsActionPressed(RehabGame.Pad_Circle))
        {
            if (!isCrouched && CrawlSpeed> 0f && onFloor && slideTimer <= 0f)
            {
                isCrouched = true;
                DoAnimation(Crash.Anim.CrouchIdle, true);
            }
        }
        if (Input.IsActionJustReleased(RehabGame.Pad_Circle))
        {
            if (isCrouched)
            {
                isCrouched = false;
            }
        }

        currentSpeed = RunSpeed;
        if (dirLength < 0.3f)
        {
            currentSpeed = 0;
        }
        else if (dirLength < 0.8f)
        {
            currentSpeed = WalkSpeed;
        }
        if (isCrouched && pressed)
        {
            currentSpeed = CrawlSpeed;
        }
        if (slideTimer > 0f)
        {
            currentSpeed = SlideSpeed;
        }
        direction = direction.Normalized();
        if (slideTimer > 0f)
        {
            direction = slideDir;
            pressed = true;
        }
        else if (isSlideJumping)
        {
            direction = slideDir;
            pressed = true;
            currentSpeed = SlideJumpSpeed;
        }
        if (slideJumpLandTimer > 0f && (pressed || isJumping || spinTimer > 0f || slideTimer > 0f))
        {
            slideJumpLandTimer = 0f;
        }

        if (pressed)
        {
            char_velocity.X = direction.X * currentSpeed;
            char_velocity.Z = direction.Z * currentSpeed;
            float atan = (float)Math.Atan2(direction.X, direction.Z);
            var targetRot = new Vector3(0, atan, 0);
            if (currentSpeed != 0)
            {
                GlobalRotation = targetRot;
            }
            else
            {
                GlobalRotation = GlobalRotation.Lerp(targetRot, 5f * delta); //GlobalRotation.Slerp(targetRot, 5f * delta);
                GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
            }
            if (!isJumping && onFloor && spinTimer <= 0.0 && slideTimer <= 0.0 && slideJumpLandTimer <= 0f)
            {
                if (isCrouched)
                {
                    if (currentSpeed == 0)
                    {
                        DoAnimation(Crash.Anim.CrouchIdle, true);
                    }
                    else
                    {
                        DoAnimation(Crash.Anim.Crawl, true);
                    }
                }
                else if (currentSpeed == 0)
                {
                    DoAnimation(Crash.Anim.ShuffleFeet, true);
                }
                else if (currentSpeed == WalkSpeed)
                {
                    DoAnimation(pushTimer > 0.0f ? Crash.Anim.WalkRoll : Crash.Anim.Walk, true);
                }
                else
                {
                    DoAnimation(pushTimer > 0.0f ? Crash.Anim.RunRoll : Crash.Anim.Run, true);
                }
            }
        }
        else
        {
            if (slideTimer <= 0f)
            {
                char_velocity.X = 0f;
                char_velocity.Z = 0f;
            }
            if (!isJumping && onFloor && spinTimer <= 0f && slideJumpLandTimer <= 0f)
            {
                if (!isCrouched && slideTimer <= 0f)
                {
                    DoAnimation(Crash.Anim.Idle, true);
                }
                else
                {
                    if (slideTimer <= 0f)
                    {
                        DoAnimation(Crash.Anim.CrouchIdle, true);
                    }
                    else
                    {
                        DoAnimation(Crash.Anim.SlidePose, true);
                    }
                }
            }
        }

        if (!isJumping && !onFloor)
        {
            coyoteTimer -= delta;
            if (coyoteTimer <= 0f && spinTimer <= 0.0 && slideTimer <= 0.0)
            {
                DoAnimation(Crash.Anim.JumpFall, false);
            }
        }

        if (!onFloor)
        {
            if (gravityOn && (coyoteTimer <= 0f || isJumping))
            {
                char_velocity.Y -= AirGravity * delta;
            }
            else
            {
                char_velocity.Y = 0f;
            }
        }
        else
        {
            coyoteTimer = 0.1f;
        }

        if (onFloor && !isJumping)
        {
            TryStepUp(delta);
        }

        float preMoveY = GlobalPosition.Y;
        bool wasGrounded = onFloor && !isJumping;

        Set("velocity", char_velocity);
        Call("move_and_slide");

        if (char_velocity.Y <= 0.2f && onFloor)
        {
            if (isSlideJumping)
            {
                DoAnimation(Crash.Anim.JumpLand2, false);
                if (ActiveAnim == Crash.Anim.JumpLand2)
                {
                    slideJumpLandTimer = (float)SubModels[ActiveModel].GetNode<AnimationPlayer>("AnimationPlayer").CurrentAnimationLength;
                }
            }
            isJumping = false;
            isSlideJumping = false;
            timesJumped = 0;
        }

        if (spinTimer > 0f)
        {
            UpdateSpinHits();
        }

        var colCount1 = (int)Call("get_slide_collision_count");
        var pushed = new HashSet<Node3D>();
        bool hitPushable = false;
        for (int i = 0; i < colCount1; i++)
        {
            var colData = (KinematicCollision3D)Call("get_slide_collision", i);
            if (colData == null) continue;
            var colCount = colData.GetCollisionCount();
            if (colCount == 0) continue;
            for (int a = 0; a < colCount; a++)
            {
                var hit = colData.GetCollider(a);
                if (hit is Node3D rigid && rigid.IsClass("RigidBody3D") && !(bool)rigid.Get("freeze"))
                {
                    hitPushable = true;
                    if (spinTimer <= 0f && pushed.Add(rigid))
                    {
                        PushRigidBody(rigid, colData.GetNormal(a), colData.GetPosition(a));
                    }
                    continue;
                }
                if (hit is Area3D area && area.GetParent() is Agent agent)
                {
                    hit = agent;
                }
                if (hit is AgentCrate crate)
                {
                    crate.OnBodyEntered(this);
                }
                else if (hit is Agents.Furniture.School.Boiler.DoubleDoor_Anim furn)
                {
                    furn.OnDoorTouch(this);
                }
            }
        }

        if (wasGrounded && (hitPushable || pushTimer > 0f) && GlobalPosition.Y > preMoveY)
        {
            groundedByPush = SnapDownOffPushable(GlobalPosition.Y - preMoveY);
        }
    }

    void EndSlide()
    {
        slideTimer = 0f;
        slideJumpBuffered = false;
        Call("set_collision_mask_value", 9, true);
        Call("set_collision_mask_value", 10, true);
        Call("set_collision_mask_value", 11, true);
        Call("set_collision_mask_value", 12, true);
        Call("set_collision_mask_value", 13, true);
        Call("set_collision_mask_value", 14, true);
    }

    void PushRigidBody(Node3D body, Vector3 normal, Vector3 point)
    {
        var pushDir = -normal;
        pushDir.Y = 0f;
        if (pushDir.LengthSquared() < 0.0001f) return;
        pushDir = pushDir.Normalized();

        if (char_velocity.Dot(pushDir) > 0f)
        {
            pushTimer = PushAnimGrace;
            lastPushedBody = body;
        }

        float velDiff = char_velocity.Dot(pushDir) - ((Vector3)body.Get("linear_velocity")).Dot(pushDir);
        if (velDiff <= 0f) return;

        float effectiveMass = Math.Min((float)body.Get("mass"), PushMass);
        body.Call("apply_impulse", pushDir * velDiff * effectiveMass * PushStrength, point - body.GlobalPosition);
    }

    bool SnapDownOffPushable(float lift)
    {
        var col = new KinematicCollision3D();
        float probe = lift + 0.1f;
        if ((bool)Call("test_move", GlobalTransform, Vector3.Down * probe, col))
        {
            var below = col.GetCollider() as Node3D;
            bool belowIsPushable = below != null && below.IsClass("RigidBody3D") && !(bool)below.Get("freeze");
            if (!belowIsPushable && col.GetNormal().Y >= 0.7f)
            {
                GlobalPosition += col.GetTravel();
                return true;
            }
        }    
        GlobalPosition += Vector3.Down * lift;
        return true;
    }

    static bool IsStaticGround(GodotObject collider)
    {
        if (collider is not Node node || node.GetClass() != "StaticBody3D") return false;
        for (var n = node; n != null; n = n.GetParent())
        {
            if (n is Agent) return false;
        }
        return true;
    }

    void TryStepUp(float delta)
    {
        var motion = new Vector3(char_velocity.X, 0f, char_velocity.Z) * delta;
        if (StepHeight <= 0f || motion.LengthSquared() < 0.000001f) return;
        var forward = motion.Normalized() * Math.Max(motion.Length(), 0.1f);

        var from = GlobalTransform;
        var col = new KinematicCollision3D();
        if (!(bool)Call("test_move", from, forward, col)) return;
        if (!IsStaticGround(col.GetCollider())) return;
        if (col.GetNormal().Y > 0.7f) return;

        var up = Vector3.Up * StepHeight;
        if ((bool)Call("test_move", from, up)) return;
        var raised = from.Translated(up);
        if ((bool)Call("test_move", raised, forward)) return;
        var landCol = new KinematicCollision3D();
        var raisedFwd = raised.Translated(forward);
        if (!(bool)Call("test_move", raisedFwd, -up, landCol)) return;
        if (landCol.GetNormal().Y < 0.7f) return;
        if (!IsStaticGround(landCol.GetCollider())) return;

        float stepAmount = StepHeight - landCol.GetTravel().Length();
        if (stepAmount <= 0.01f) return;
        GlobalPosition += Vector3.Up * stepAmount;
    }

    void UpdateSpinHits()
    {
        if (spinHitShape == null) spinHitShape = new SphereShape3D();
        spinHitShape.Radius = SpinHitRadius;
        var query = new PhysicsShapeQueryParameters3D();
        query.Shape = spinHitShape;
        query.Transform = new Transform3D(Basis.Identity, GlobalPosition + (Vector3.Up * 0.9f));
        query.CollisionMask = (uint)Call("get_collision_mask");
        query.Exclude = new Godot.Collections.Array<Rid>() { (Rid)Call("get_rid") };

        var results = GetWorld3D().DirectSpaceState.IntersectShape(query);
        foreach (var result in results)
        {
            if (result["collider"].AsGodotObject() is not Node3D body) continue;
            if (!body.IsClass("RigidBody3D") || (bool)body.Get("freeze")) continue;
            if (!spinHitBodies.Add(body)) continue;
            SpinHitRigidBody(body);
        }
    }

    void SpinHitRigidBody(Node3D body)
    {
        var hitDir = body.GlobalPosition - GlobalPosition;
        hitDir.Y = 0f;
        if (hitDir.LengthSquared() < 0.0001f)
        {
            hitDir = GlobalTransform.Basis.Z;
            hitDir.Y = 0f;
        }
        hitDir = hitDir.Normalized();

        var targetVel = (hitDir * SpinHitSpeed) + (Vector3.Up * SpinHitLift);
        var velChange = targetVel - (Vector3)body.Get("linear_velocity");
        float mass = (float)body.Get("mass");
        float effectiveMass = Math.Min(mass, PushMass);
        body.Call("apply_central_impulse", velChange * effectiveMass);
        body.Call("apply_torque_impulse", Vector3.Up.Cross(hitDir) * effectiveMass * 2f);
    }

    void UpdateHeadAnim(float delta)
    {
        if (headdirX > 0f)
        {
            headdirX -= delta * 0.4f;
            headdirX = Math.Clamp(headdirX, 0f, 0.8f);
        }
        else
        {
            headdirX += delta * 0.4f;
            headdirX = Math.Clamp(headdirX, -0.8f, 0f);
        }

        if (headdirY > 0f)
        {
            headdirY -= delta * 0.5f;
            headdirY = Math.Clamp(headdirY, 0f, 1.0f);
        }
        else
        {
            headdirY += delta * 0.75f;
            headdirY = Math.Clamp(headdirY, -1.5f, 0f);
        }

        var oldX = headdirX;
        var oldY = headdirY;
        if (Input.IsActionPressed(RehabGame.Pad_RStick_Left))
        {
            if (!RehabGame.InvertCameraX)
            {
                headdirX -= Input.GetActionStrength(RehabGame.Pad_RStick_Left) * delta * 4.0f;
            }
            else
            {
                headdirX += Input.GetActionStrength(RehabGame.Pad_RStick_Left) * delta * 4.0f;
            }
        }
        if (Input.IsActionPressed(RehabGame.Pad_RStick_Right))
        {
            if (!RehabGame.InvertCameraX)
            {
                headdirX += Input.GetActionStrength(RehabGame.Pad_RStick_Right) * delta * 4.0f;
            }
            else
            {
                headdirX -= Input.GetActionStrength(RehabGame.Pad_RStick_Right) * delta * 4.0f;
            }
        }
        if (Input.IsActionPressed(RehabGame.Pad_RStick_Up))
        {
            if (!RehabGame.InvertCameraY)
            {
                headdirY += Input.GetActionStrength(RehabGame.Pad_RStick_Up) * delta * 4.0f;
            }
            else
            {
                headdirY -= Input.GetActionStrength(RehabGame.Pad_RStick_Up) * delta * 4.0f;
            }
        }
        if (Input.IsActionPressed(RehabGame.Pad_RStick_Down))
        {
            if (!RehabGame.InvertCameraY)
            {
                headdirY -= Input.GetActionStrength(RehabGame.Pad_RStick_Down) * delta * 4.0f;
            }
            else
            {
                headdirY += Input.GetActionStrength(RehabGame.Pad_RStick_Down) * delta * 4.0f;
            }
        }

        if (isCrouched || slideTimer > 0f)
        {
            headdirX = oldX;
            headdirY = oldY;
        }
        headdirX = Math.Clamp(headdirX, -0.8f, 0.8f);
        headdirY = Math.Clamp(headdirY, -1.5f, 1.0f);

        SubModels[ActiveModel].GetNode<AnimationPlayer>("AnimationPlayer").CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        SubModels[ActiveModel].GetNode<AnimationPlayer>("AnimationPlayer").Advance(delta);
        if (ActiveSkeleton != null && JointsConst[2] != -1 && ActiveSkeleton.GetBoneCount() > JointsConst[2])
        {
            var headBoneRot = ActiveSkeleton.GetBonePoseRotation(JointsConst[2]);
            var headBoneEuler = headBoneRot.GetEuler();
            headBoneEuler.X += headdirY;
            headBoneEuler.Y += headdirX;
            //headBoneEuler.Z -= headdirX;
            ActiveSkeleton.SetBonePoseRotation(JointsConst[2], Quaternion.FromEuler(headBoneEuler));
        }
    }

    public void UpdateFootStep(float delta)
    {
        if (ActiveAnim != Crash.Anim.Run && ActiveAnim != Crash.Anim.Walk) return;

        footsteptimer -= delta;
        if (footsteptimer > 0f) return;

        footsteplast = !footsteplast;
        var clip1 = FS_Dirt_1;
        var clip2 = FS_Dirt_2;
        var space_state = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition + (Vector3.Up * 1.0f), GlobalPosition + (Vector3.Up * -3.0f));
        var rid = (Rid)Call("get_rid");
        query.Exclude = new Godot.Collections.Array<Rid>() { rid };
        var result = space_state.IntersectRay(query);
        if (result.ContainsKey("collider"))
        {
            var body = (Node)result["collider"];
            switch (body.GetParent().Name)
            {
                case "Normal_Rock":
                    clip1 = FS_Stone_1;
                    clip2 = FS_Stone_2;
                    break;
                case "Normal_Grass":
                    clip1 = FS_Grass_1;
                    clip2 = FS_Grass_2;
                    break;
                case "Normal_Metal":
                case "Slippy_Metal":
                    clip1 = FS_Metal_1;
                    clip2 = FS_Metal_2;
                    break;
                case "Normal_Wood":
                    clip1 = FS_Wood_1;
                    clip2 = FS_Wood_2;
                    break;
                case "Normal_Sand":
                case "Normal_Snow":
                    clip1 = FS_Sand_1;
                    clip2 = FS_Sand_2;
                    break;
                case "Default":
                case "Normal_Mud":
                case "Generic_MediumSlippy":
                case "Lava":
                case "Slippy_Rock":
                case "Sticky_Snow":
                case "Ice":
                case "Ice_LowSlippy":
                case "Generic_MediumSlippy_RigidOnly":
                    clip1 = FS_Dirt_1;
                    clip2 = FS_Dirt_2;
                    break;
                case "Normal_Water":
                    clip1 = FS_Water_1;
                    clip2 = FS_Water_2;
                    break;
                case "Normal_StoneTiles":
                    clip1 = FS_Tile_1;
                    clip2 = FS_Tile_2;
                    break;
                case "Generic_SlightlySlippy":
                case "HackRail":
                    clip1 = FS_Slippy;
                    clip2 = FS_Slippy;
                    break;
                default:
                    clip1 = FS_Stone_1;
                    clip2 = FS_Stone_2;
                    break;
            }

            if (footsteplast)
            {
                clip1 = clip2;
            }
            DoSoundStream(clip1, 1.0f, -5.0f);
            if (ActiveAnim == Crash.Anim.Walk)
            {
                footsteptimer = 0.5f;
            }
            else
            {
                footsteptimer = 0.25f;
            }
        }

    }

    public void UpdateDynamicCollision(float delta)
    {
        //Aabb box = new Aabb();
        //UpdateDynamicColNested(SubModels[ActiveModel], ref box);

        float MinX = 10000f, MinZ = 10000f, MaxX = -10000f, MaxY = -10000f, MaxZ = -10000f; ;
        for (int i = 0; i < ActiveSkeleton.GetBoneCount(); i++)
        {
            var pos = ActiveSkeleton.GetBoneGlobalPose(i);
            if (pos.Origin.X < MinX) MinX = pos.Origin.X;
            if (pos.Origin.Z < MinZ) MinZ = pos.Origin.Z;
            if (pos.Origin.X > MaxX) MaxX = pos.Origin.X;
            if (pos.Origin.Y > MaxY) MaxY = pos.Origin.Y;
            if (pos.Origin.Z > MaxZ) MaxZ = pos.Origin.Z;
        }
        if (spinTimer > 0f)
        {
            MaxY = 1.8f;
            MinX = -1.25f;
            MaxX = 1.25f;
            MinZ = -1.25f;
            MaxZ = 1.25f;
        }
        /*
        Vector3 center = new Vector3(0f, MaxY / 2f, 0f);
        Vector3 size = new Vector3((MaxX - MinX) + 0.1f, MaxY, (MaxZ - MinZ) + 0.1f);
        DynamicCol.Position = DynamicCol.Position.MoveToward(center, delta * 10f);
        var oldSize = (Vector3)DynamicCol.Shape.Get("size");
        DynamicCol.Shape.Set("size", oldSize.MoveToward(size, delta * 10f));
        */
        Vector3 size = new Vector3((MaxX - MinX) + 0.1f, MaxY, (MaxZ - MinZ) + 0.1f);
        var oldPts = (Vector3[])DynamicCol.Shape.Get("points");
        Vector3[] Points = [
            oldPts[0].MoveToward(new Vector3(0.5f, 0, 0.5f) * size, 10f * delta),
            oldPts[1].MoveToward(new Vector3(-0.5f, 0, -0.5f) * size, 10f * delta),
            oldPts[2].MoveToward(new Vector3(0.5f, 0, -0.5f) * size, 10f * delta),
            oldPts[3].MoveToward(new Vector3(-0.5f, 0, 0.5f) * size, 10f * delta),
            oldPts[4].MoveToward(new Vector3(0.5f, 1, 0.5f) * size, 10f * delta),
            oldPts[5].MoveToward(new Vector3(-0.5f, 1, -0.5f) * size, 10f * delta),
            oldPts[6].MoveToward(new Vector3(0.5f, 1, -0.5f) * size, 10f * delta),
            oldPts[7].MoveToward(new Vector3(-0.5f, 1, 0.5f) * size, 10f * delta),
        ];
        DynamicCol.Shape.Set("points", Points);

        //var oldSize = (Vector3)DynamicCol.Shape.Get("size");
        //DynamicCol.Position = box.Position + (box.Size / 2f);
        //DynamicCol.Shape.Set("size", oldSize.MoveToward(box.Size, delta * 10f));
        //DynamicCol.Shape.Set("size", box.Size);
        //DynamicColVis.Position = DynamicCol.Position;
        //DynamicColVis.Mesh.Set("size", size);
    }

    void UpdateDynamicColNested(Node parent, ref Aabb inBox)
    {
        if (parent is MeshInstance3D mesh)
        {
            inBox = inBox.Merge(mesh.Mesh.GetAabb());
        }
        foreach (var i in parent.GetChildren())
        {
            UpdateDynamicColNested(i, ref inBox);
        }
    }

    void UpdateNPC(float delta)
    {
        DoAnimation(Crash.Anim.Idle, true);
    }
    
    void XR_Setup()
    {
        RehabScene.Root.XR_Origin.XR_Camera.Position = Vector3.Zero;
        RehabScene.Root.XR_Origin.GlobalPosition = GlobalPosition + (Vector3.Up * RehabGame.XR_Height);
        RehabScene.Root.XR_Origin.GlobalRotationDegrees = new Vector3(GlobalRotationDegrees.X, GlobalRotationDegrees.Y + 180f, GlobalRotationDegrees.Z);
        XRServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
        Visible = false;
        string RHandPath = "res://assets/scenes/xr/XRHand_Crash.tscn";
        string LHandPath = "res://assets/scenes/xr/XRHand_Crash.tscn";
        switch (CharType)
        {
            default: break;
            case CharacterType.Cortex:
                RHandPath = "res://assets/scenes/xr/XRHand_Cortex.tscn";
                LHandPath = "res://assets/scenes/xr/XRHand_Cortex.tscn";
            break;
            case CharacterType.Coco:
                RHandPath = "res://assets/scenes/xr/XRHand_Crunch_Metal.tscn";
                LHandPath = "res://assets/scenes/xr/XRHand_Crunch.tscn";
            break;
            case CharacterType.Nina:
                RHandPath = "res://assets/scenes/xr/XRHand_Nina.tscn";
                LHandPath = "res://assets/scenes/xr/XRHand_Nina.tscn";
            break;
            case CharacterType.EvilCrash:
                RHandPath = "res://assets/scenes/xr/XRHand_EvilCrash.tscn";
                LHandPath = "res://assets/scenes/xr/XRHand_EvilCrash.tscn";
            break;
            case CharacterType.MechaBandicoot:
                RHandPath = "res://assets/scenes/xr/XRHand_Mecha_Chainsaw.tscn";
                LHandPath = "res://assets/scenes/xr/XRHand_Mecha_Rocket.tscn";
            break;
        }
        RehabScene.Root.XR_Origin.XR_HandL.SpawnHand(LHandPath, this);
        RehabScene.Root.XR_Origin.XR_HandR.SpawnHand(RHandPath, this);
        //AudioSource.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
    }
}