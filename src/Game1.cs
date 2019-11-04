using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace simple_game
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        (int width, int height) WindowSize = (640, 640);
        const int zoom = 4;
        Rectangle world;
        Player player;
        Turret turret;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            world = new Rectangle(0, 0, WindowSize.width / zoom, WindowSize.height / zoom);

            graphics.PreferredBackBufferHeight = WindowSize.height;
            graphics.PreferredBackBufferWidth = WindowSize.width;
        }

        protected override void Initialize()
        {
            base.Initialize();
            player = new Player(Vector2.Zero);
            turret = new Turret(new Vector2(5, 5) * 16);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Loader.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyState.IsKeyDown(Keys.Escape))
                Exit();

            turret.Update(player.position, keyState);
            player.Update(keyState, world);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Draw Background
            GraphicsDevice.Clear(Color.Azure);

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Immediate,
                samplerState: SamplerState.PointClamp, // Helps retain crispness for pixel graphics
                transformMatrix: Matrix.CreateScale(zoom) // Use zoom
            );

            player.Draw(spriteBatch);
            turret.Draw(spriteBatch);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    public enum Textures { Turret, Player, Bullet }

    public static class Loader
    {
        public static Dictionary<Textures, (Texture2D, Vector2)> textures;

        public static void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            Texture2D texture = content.Load<Texture2D>("Tileset");
            // Include tileset texture and refer to location on tileset
            textures = new Dictionary<Textures, (Texture2D, Vector2)> {
                {Textures.Turret, (texture, new Vector2(2, 0))},
                {Textures.Player, (texture, new Vector2(1, 0))},
                {Textures.Bullet, (texture, new Vector2(0, 1))},
            };
        }
    }

    public class Object
    {
        public Vector2 position;
        public float rotation;
        public float scale = 1;
        protected (Texture2D texture, Vector2 position) _sprite;
        public readonly Vector2 spritePosition = new Vector2();

        public Object(Vector2 position, float rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public void Draw(SpriteBatch spriteBatch) =>
            spriteBatch.Draw(
                this._sprite.texture,
                this.position,
                new Rectangle((16 * this._sprite.position).ToPoint(),
                (Vector2.One * 16).ToPoint()),
                Color.White,
                this.rotation,
                Vector2.One * this.scale * 8,
                Vector2.One * this.scale,
                SpriteEffects.None,
                0
            );
    }

    public class Player : Object
    {
        public const float speed = 1; // Player speed

        public Player(Vector2 position) : base(position, 0) { this._sprite = Loader.textures[Textures.Player]; }

        public void Update(KeyboardState keyState, Rectangle world)
        {
            Vector2 newPosition = new Vector2(this.position.X, this.position.Y);
            if (keyState.IsKeyDown(Keys.Up))
                newPosition -= new Vector2(0, speed);
            else if (keyState.IsKeyDown(Keys.Down))
                newPosition += new Vector2(0, speed);
            if (keyState.IsKeyDown(Keys.Left))
                newPosition -= new Vector2(speed, 0);
            else if (keyState.IsKeyDown(Keys.Right))
                newPosition += new Vector2(speed, 0);

            // Prevent player leaving window
            if (world.Contains(newPosition))
                this.position = newPosition;
        }
    }

    public class Turret : Object
    {
        private int _cooldown; // The number of ticks before another bullet can be shot
        public List<Bullet> bullets = new List<Bullet>();

        public Turret(Vector2 position) : base(position, 0) { _cooldown = 30; this._sprite = Loader.textures[Textures.Turret]; }

        public void shoot() => bullets.Add(new Bullet(this.position, this.rotation));

        private void pointAt(Vector2 target) =>
            base.rotation = (float)Math.Atan2(this.position.Y - target.Y, this.position.X - target.X);

        public void Update(Vector2 target, KeyboardState keyState)
        {
            this.pointAt(target);
            this.bullets.ForEach(bullet => bullet.Update()); // Progress each bullet
            this.bullets.RemoveAll(bullet => !bullet.isValid); // Deletes bullets older than 200 ticks

            if (keyState.IsKeyDown(Keys.Space) && _cooldown <= 0) { this.shoot(); _cooldown = 30; }
            else if (_cooldown > 0) { _cooldown--; }
        }

        public new void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            this.bullets.ForEach(bullet => bullet.Draw(spriteBatch));
        }

        public class Bullet : Object
        {
            protected Vector2 _direction;
            protected int life;
            public bool isValid = true;
            private const float _bulletSpeed = 2;
            public new float scale = .2f;
            private const int _maxAge = 200;

            public Bullet(Vector2 position, float direction) : base(position, direction)
            {   
                // Convert angular direction to velocity vector
                this._direction = new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction)) * -_bulletSpeed;
                this._sprite = Loader.textures[Textures.Bullet];
            }

            public void Update()
            {
                this.position += _direction;
                // If bullet has been alive for more than 200 ticks set it to be invalid
                if (this.life++ > _maxAge) isValid = false;
            }
        }
    }
}