using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace TheSanity.GlobalNPC.Bosses.Twinkle
{
    public class StarmerangProj : ModProjectile
    {
        public override string Texture => "TheSanity/GlobalNPC/Bosses/Twinkle/Starmerang";

        private int AI_State {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        private int TargetIndex {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private int shootTimer = 0;
        private int shotCounter = 0;
        private int maxShots = 0;
        private float orbitTimer = 0f;

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; 
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            
            if (maxShots == 0) {
                maxShots = Main.rand.Next(5, 8);
            }

            Projectile.rotation += 0.4f * Projectile.direction;

            // FASE 0: TERBANG MENCARI MUSUH
            if (AI_State == 0) {
                NPC closestNPC = null;
                float closestDistance = 550f;

                for (int i = 0; i < Main.maxNPCs; i++) {
                    NPC npc = Main.npc[i];
                    if (npc.CanBeChasedBy(Projectile)) {
                        float distance = Vector2.Distance(Projectile.Center, npc.Center);
                        if (distance < closestDistance) {
                            closestNPC = npc;
                            closestDistance = distance;
                        }
                    }
                }

                if (closestNPC != null) {
                    TargetIndex = closestNPC.whoAmI;
                    AI_State = 1;
                    Projectile.netUpdate = true;
                }

                if (Projectile.timeLeft < 3600 - 45) {
                    AI_State = 2;
                }
            }
            // FASE 1: MENGORBIT & MENEMBAK
            else if (AI_State == 1) {
                NPC target = Main.npc[TargetIndex];

                if (!target.active || !target.CanBeChasedBy(Projectile)) {
                    AI_State = 2;
                    Projectile.netUpdate = true;
                    return;
                }

                orbitTimer += 0.07f; 

                float patternSpread = (Projectile.identity % 3) * (MathHelper.TwoPi / 3f);
                float finalAngle = orbitTimer + patternSpread;
                float orbitRadius = 75f; 

                Vector2 desiredPos = target.Center + finalAngle.ToRotationVector2() * orbitRadius;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredPos - Projectile.Center, 0.25f);

                shootTimer++;
                if (shootTimer >= 20) { 
                    shootTimer = 0;
                    shotCounter++;

                    if (Main.myPlayer == Projectile.owner) {
                        Vector2 shootVel = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 7f;
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(), 
                            Projectile.Center, 
                            shootVel, 
                            ModContent.ProjectileType<StarmerangStar>(), 
                            (int)(Projectile.damage * 0.65f), 
                            0.5f, 
                            Projectile.owner, 
                            target.whoAmI 
                        );
                    }

                    if (shotCounter >= maxShots) {
                        AI_State = 2;
                        Projectile.netUpdate = true;
                    }
                }
            }
            // FASE 2: PULANG
            else if (AI_State == 2) {
                Vector2 returnDirection = player.Center - Projectile.Center;
                float distanceToPlayer = returnDirection.Length();

                if (distanceToPlayer < 25f) {
                    Projectile.Kill();
                    return;
                }

                returnDirection.Normalize();
                Projectile.velocity = returnDirection * 17f;
            }
        }

        public override bool PreDraw(ref Color drawColor) {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            float correctedRotation = Projectile.rotation + MathHelper.Pi;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, drawColor, 
                correctedRotation, origin, Projectile.scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            
            return false; 
        }
    }
}