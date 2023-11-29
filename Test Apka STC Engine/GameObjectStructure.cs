﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STCEngine
{
    #region Components
    /// <summary>
    /// Base for all components, components define the properties of GameObjects
    /// </summary>
    [JsonConverter(typeof(ComponentConverter))]
    public abstract class Component
    {
        public abstract string Type { get; }
        public bool enabled { get; set; } = true;
        /// <summary>
        /// the GameObject this component is attached to
        /// </summary>
        [JsonIgnore] public GameObject gameObject { get; set; }

        /// <summary>
        /// Function that gets called upon creating this component
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Function that gets called upon destroying this component or the GameObject its attached to
        /// </summary>
        public abstract void DestroySelf();
        [JsonConstructor]protected Component() { }
    }



    /// <summary>
    /// A component responsible for position, rotation and scale of the GameObject
    /// </summary>
    public class Transform : Component
    {
        public override string Type { get; } = nameof(Transform);
        public Vector2 position { get; set; } = Vector2.one;
        public float rotation { get; set; }
        public Vector2 size { get; set; }

        /// <summary>
        /// Creates a Transform component with a given position, rotation and size
        /// </summary>
        public Transform(Vector2 position, float rotation, Vector2 size)
        {
            this.position = position == null ? Vector2.zero : position;
            this.rotation = rotation;
            this.size = size == null ? Vector2.one : size;
        }
        public override void DestroySelf()
        {
            Debug.LogWarning("Tried to destroy Transform component, destroying the whole GameObject");
            gameObject.DestroySelf();
        }
        public override void Initialize(){}
    }

    /// <summary>
    /// A component responsible for holding visual information about the GameObject
    /// </summary>
    public class Sprite : Component
    {
        public override string Type { get; } = nameof(Sprite);
        [JsonIgnore] private Image? _image; 
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set => _image = value; }
        public int orderInLayer { get; private set; }
        public string fileSourceDirectory { get; set; }
        public Sprite(string fileSourceDirectory, int orderInLayer = int.MaxValue)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this.orderInLayer = orderInLayer;
        }
        //[JsonConstructor] public Sprite() { } //not needed
        public override void Initialize() 
        {
            EngineClass.AddSpriteToRender(gameObject, orderInLayer);
            this.orderInLayer = EngineClass.spritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            if (gameObject.GetComponent<Sprite>() != null) { gameObject.RemoveComponent<Sprite>(); return; }
            EngineClass.RemoveSpriteToRender(gameObject);
        }
    }
    /// <summary>
    /// A component responsible for rendering UI elements, essentially a basic sprite but handled differently while rendering
    /// </summary>
    public class UISprite : Component
    {
        public override string Type { get; } = nameof(UISprite);
        [JsonIgnore] private Image? _image;
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set => _image = value; }
        public int orderInUILayer { get; private set; }
        public string fileSourceDirectory { get; set; }
        public UISprite(string fileSourceDirectory, int orderInLayer = int.MaxValue)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this.orderInUILayer = orderInLayer;
        }
        //[JsonConstructor] public Sprite() { } //not needed
        public override void Initialize()
        {
            EngineClass.AddUISpriteToRender(gameObject, orderInUILayer);
            this.orderInUILayer = EngineClass.UISpritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            if (gameObject.GetComponent<UISprite>() != null) { gameObject.RemoveComponent<UISprite>(); return; }
            EngineClass.RemoveUISpriteToRender(gameObject);
        }
    }
    /// <summary>
    /// A component responsible for rendering a grid of images
    /// </summary>
    public class Tilemap : Component
    {
        public override string Type { get; } = nameof(Tilemap);
        public int orderInLayer { get; private set; }//higher numbers render on top of lower numbers
        private Dictionary<string, string> tileSources { get; set; }
        public string[] tilemapString { get; set; }
        public Image[,] tiles { get; set; }
        public Vector2 tileSize { get; set; }
        public Vector2 mapSize { get; set; }

        //to edit the origin, move the gameObject the tilemap is attached to
        public Tilemap(string jsonSourcePath, int orderInLayer = 0)
        {
            //loads the json into a new object
            string text = File.ReadAllText(jsonSourcePath);
            var tilemapValues = JsonSerializer.Deserialize<TilemapValues>(text);

            //copies all the values from that object
            tilemapString = tilemapValues.tilemapString;

            tileSources = tilemapValues.tileSources;
            tileSize = new Vector2(tilemapValues.tileWidth, tilemapValues.tileHeight);
            mapSize = new Vector2(tilemapValues.mapWidth, tilemapValues.mapHeight);

            //creates the tilemap
            CreateTilemap();
        }
        /// <summary>
        /// Creates tiles array and adds it to the render queue
        /// </summary>
        private void CreateTilemap()
        {
            try
            {
                tiles = new Image[(int)mapSize.x, (int)mapSize.y];
                //creates the user key + path dictionary (ex. grass -> Assets/GrassTexture.png)
                //CreateDictionary();

                for (int y = 0; y < mapSize.y; y++)
                {
                    for (int x = 0; x < mapSize.x; x++)
                    {
                        tiles[x, y] = tileSources.TryGetValue(tilemapString[x + y * (int)mapSize.x], out string? value) ? Image.FromFile(value) : throw new Exception("bambusovina");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error creating tilemap, error message: " + e.Message);
            }
        }

        private class TilemapValues
        {
            public string[] tilemapString { get; set; }
            public Dictionary<string, string> tileSources { get; set; }
            public float mapWidth { get; set; }
            public float mapHeight { get; set; }
            public float tileWidth { get; set; }
            public float tileHeight { get; set; }
        }
        [JsonConstructor] public Tilemap(){}
        public override void Initialize() 
        {
            EngineClass.AddSpriteToRender(gameObject, orderInLayer);
            this.orderInLayer = EngineClass.spritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            EngineClass.RemoveSpriteToRender(gameObject);
        }
    }
    
    #region Animation-related components and classes
    /// <summary>
    /// A component responsible for animating a gameobject with a sprite component
    /// </summary>
    public class Animator : Component
    {
        public override string Type { get; } = nameof(Animator);
        [JsonIgnore] public Sprite? sprite { get; set; }
        public Dictionary<string, Animation> animations { get; set; }
        //[JsonIgnore] public Dictionary<string, Animation> _animations { get; private set; }
        public float playBackSpeed { get; set; }
        [JsonIgnore] public Animation? currentlyPlayingAnimation { get; private set; }
        [JsonIgnore] public bool isPlaying { get => currentlyPlayingAnimation != null; }
        public Animator(Animation animation, float playBackSpeed = 1)
        {
            this.animations = new Dictionary<string, Animation>();
            animations.Add(animation.name, animation);

            this.playBackSpeed = Math.Clamp(playBackSpeed, 0, float.PositiveInfinity);
        }
        
        /// <summary>
        /// Adds an animation to the animator, which can later be played using the Play function
        /// </summary>
        /// <param name="animation"></param>
        public void AddAnimation(Animation animation) { animations.Add(animation.name, animation); }
        /// <summary>
        /// Plays the animation of the given name
        /// </summary>
        /// <param name="animationName"></param>
        public void Play(string animationName) 
        { 
            if(sprite == null) { sprite = gameObject.GetComponent<Sprite>(); }
            if (animations.TryGetValue(animationName, out Animation? animation)) { EngineClass.AddSpriteAnimation(animation); animation.sprite = sprite; currentlyPlayingAnimation = animation; animation.animator = this; } 
            else { Debug.LogError($"Animation {animationName} not found and couldnt be played."); }     
        }
        public void Stop()
        {
            try
            {
                EngineClass.RemoveSpriteAnimation(currentlyPlayingAnimation??throw new Exception("Trying to stop animator that isnt playing!"));
                currentlyPlayingAnimation = null;
            }catch(Exception e) { Debug.LogError(e.Message); }
        }


        [JsonConstructor] public Animator() { }
        public override void Initialize() {}
        public override void DestroySelf()
        {
            if(gameObject.GetComponent<Animator>() != null) { gameObject.RemoveComponent<Animator>(); return; }
        }
    }
    /// <summary>
    /// A single frame of an animation
    /// </summary>
    public class AnimationFrame
    {
        [JsonIgnore] private Image? _image;
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set => _image = value; }
        public string fileSourceDirectory { get; set; }
        /// <summary>
        /// How long the frame stays in ms
        /// </summary>
        public int time { get; set; }
        //public AnimationFrame(Image image, int time)
        //{
        //    this.image = image;
        //    this.time = time;
        //}
        
        ///<summary>
        /// Creates a frame of an animation from the source of the image and the time how long this image should stay in the animation in ms
        /// </summary>
        public AnimationFrame(String fileSourceDirectory, int time)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this.time = time;
        }
    }
    /// <summary>
    /// An animation to be used with the Animator component
    /// </summary>
    public class Animation
    {
        public string name { get; set; }
        public AnimationFrame[] animationFrames { get; set; }
        public bool loop { get; set; }
        [JsonIgnore] public Animator animator { get; set; }
        [JsonIgnore] private int timer { get; set; }
        [JsonIgnore] private int nextFrameTimer{ get; set; }
        [JsonIgnore] private int animationFrame { get; set; }
        [JsonIgnore] public Sprite? sprite { get; set; }

    /// <summary>
    /// Internal function, should NEVER be called by the user!
    /// To start an animation, call the "Play" function in the animator component!
    /// </summary>
    public void RunAnimation()
        {
            if(sprite == null) { Debug.LogError("Animation sprite not found (was the RunAnimation function called manually? To play an animation, use the \"Play\" function in the Animator component!)"); }
            if(timer > nextFrameTimer)
            {
                //Debug.Log(timer.ToString() + ", " + animationFrame);
                if(animationFrame < animationFrames.Count() - 1)
                {
                    sprite.image = animationFrames[animationFrame+1].image;
                    nextFrameTimer = animationFrames[animationFrame + 1].time;
                    timer = 0;
                    animationFrame++;
                }
                else if(loop)
                {
                    sprite.image = animationFrames[0].image;
                    nextFrameTimer = animationFrames[0].time;
                    timer = 0;
                    animationFrame = 0;
                }
                else
                {
                    animator.Stop();
                }
            }
            timer+=10;
        }

        public Animation(string name, AnimationFrame[] animationFrames, bool loop)
        {
            this.name = name;
            this.animationFrames = animationFrames;
            this.loop = loop;
            timer = 0; nextFrameTimer = animationFrames[0].time; animationFrame = 0;
        }
    }
    #endregion
    
    /// <summary>
    /// Base class for all components responsible for detecting collisions
    /// </summary>
    public abstract class Collider : Component
    {
        public bool isTrigger { get; set; } //whether it stops movement upon collision
        public Vector2 offset { get; set; }
        public abstract bool IsColliding(BoxCollider other); //udelat override v tehle classe i pro circle, elipsu,...
        public abstract Collider[] OverlapCollider(bool includeTriggers = false);
        [JsonConstructor] protected Collider() { }

    }
    /// <summary>
    /// A component responsible for detecting collisions in a box area
    /// </summary>
    public class BoxCollider : Collider
    {
        public override string Type { get; } = nameof(BoxCollider);
        public Vector2 size { get; set; }

        public bool debugDraw { get; private set; }
        /// <summary>
        /// Creates the box collider of the given size and with a given offset from gameObjects position 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="offset"></param>
        /// <param name="isTrigger"></param>
        public BoxCollider(Vector2 size, Vector2? offset = null, bool isTrigger = false, bool debugDraw = false)
        {
            this.size = size;
            this.offset = offset ?? Vector2.zero;
            this.isTrigger = isTrigger;
            this.debugDraw = debugDraw;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherCollider"></param>
        /// <returns>Whether this collider overlaps with the given collider</returns>
        public override bool IsColliding(BoxCollider otherCollider)
        {
            var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;
            return ((Math.Abs(relativeDistance.x) < (size.x + otherCollider.size.x) / 2) && (Math.Abs(relativeDistance.y) < (size.y + otherCollider.size.y) / 2));
        }

        /// <summary>
        /// Returns all the colliders colliding with this one, option to include triggers
        /// </summary>
        /// <param name="includeTriggers"></param>
        /// <returns>Array of colliding colliers</returns>
        public override Collider[] OverlapCollider(bool includeTriggers = false)
        {
            List<Collider> outList = new List<Collider>();
            foreach(Collider col in EngineClass.registeredColliders)
            {
                if (col.IsColliding(this) && (!(!includeTriggers && col.isTrigger))) { outList.Add(col); }
            }
            return outList.ToArray();
        }

        [JsonConstructor] BoxCollider() { }
        public override void Initialize() 
        {
            EngineClass.RegisterCollider(this);

            if (debugDraw)
            {
                EngineClass.AddDebugRectangle(this, 0);
            }
        }
        public override void DestroySelf()
        {
            if (debugDraw) { EngineClass.RemoveDebugRectangle(this); }
            EngineClass.UnregisterCollider(this);

        }
    }
    #endregion


    /// <summary>
    /// Any object inside the game, has a name and a list of components that defines its properties
    /// </summary>
    public class GameObject
    {
        
        public List<Component> components { get; set; } = new List<Component>(); //is converted to the specific derivatives when creating a json file
        public string name { get; set; }
        /// <summary>
        /// Defines whether the object is currently active/enabled in the game, inactive GameObjects do not affect the game in any way
        /// </summary>
        public bool isActive { get => _isActive; 
            set 
            {
                isActiveChanged(!_isActive); //zatim nevyuzito :)
                _isActive = value;
            } 
        }
        private bool _isActive { get; set; }
        [JsonIgnore] public Transform transform { get; set; }

        #region Constructors and related
        [JsonConstructor] public GameObject() { }

        /// <summary>
        /// Creates a GameObject with given components
        /// </summary>
        public GameObject(string name, List<Component> components, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            foreach (Component c in components) { AddComponent(c); }
            GameObjectCreated();
        }

        /// <summary>
        /// Creates a GameObject with Transform component at given position with no rotation and scale 0
        /// </summary>
        public GameObject(string name, Vector2 position, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            AddComponent(new Transform(position, 0, Vector2.zero));
            GameObjectCreated();
        }
        /// <summary>
        /// Creates a GameObject with the given Transform component
        /// </summary>
        public GameObject(string name, Transform transform, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            AddComponent(transform);
            GameObjectCreated();
        }
        /// <summary>
        /// Creates a new GameObject from a JSON file with the given source path
        /// </summary>
        /// <param name="jsonSourcePath"></param>
        /// <returns>Reference to the new GameObject</returns>
        public static GameObject CreateGameObjectFromJSON(string jsonSourcePath)
        {
            try
            {
                string jsonString = File.ReadAllText(jsonSourcePath);
                var newGameObject = JsonSerializer.Deserialize<GameObject>(jsonString, new JsonSerializerOptions { WriteIndented = true, Converters = { new ComponentConverter() } });
                foreach (Component c in newGameObject.components)
                {
                    c.gameObject = newGameObject;
                    c.Initialize();
                }
                newGameObject.GameObjectCreated();
                return newGameObject;
            }catch(Exception e) { Debug.LogError("GameObject couldnt be created from json, error message: " + e.Message); }
            return null;
        }
        /// <summary>
        /// Called upon creating a GameObject, registers the object in the Engine class and prints a debug
        /// </summary>
        private void GameObjectCreated()
        {
            try
            {
                transform = components.OfType<Transform>().Single()/* ?? throw new Exception($"GameObject does not contain a (mandatory) Transform component")*/;
            }
            catch(Exception e)
            {
                Debug.LogError($"GameObject \"{name}\" couldn't be created, error message: " + e.Message + " (did you forget to add a Transform component?)");
                return;
            }
            EngineClass.RegisterGameObject(this);
            Debug.LogInfo($"GameObject \"{name}\" Registered");
        }
        #endregion

        public static void isActiveChanged(bool newState) { } //jeste nevyuzito :)

        #region Component Managment
        /// <summary>
        /// Adds the given component to the GameObject
        /// </summary>
        /// <param name="component"></param>
        /// <returns>The newly added componnent</returns>
        public T AddComponent<T>(T component) where T : Component
        {
            components.Add(component);
            component.gameObject = this;
            Debug.LogInfo($"Component {component} has been added onto GameObject {this.name}");
            component.Initialize();
            return component;
        }
        /// <summary>
        /// Removes the component of given type from the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        public void RemoveComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if(component == null) { Debug.LogError($"Tried removing a non-existing component {typeof(T)} from GameObject {name}!"); return; }

            Type targetType = typeof(T);
            if (targetType == typeof(Transform)) { Debug.LogWarning("Can't remove Transform component from GameObject!"); return; }
            
            components.Remove(component);
            component.DestroySelf();
            Debug.LogInfo($"Component {targetType} has been removed from GameObject {this.name}");
        }
        /// <summary>
        /// Returns a reference to the component of given type on the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        /// <returns>Reference to the specified component</returns>
        public T? GetComponent<T>() where T : Component
        {
            Type targetType = typeof(T);
            var foundComponent = components.FirstOrDefault(c => targetType.IsAssignableFrom(c.GetType()));
            
            // Use reflection to create an instance of the specified type and cast the found component to it.
            return foundComponent != null ? (T)Convert.ChangeType(foundComponent, targetType) : null;
        }
        /// <summary>
        /// Returns references to all of the components of the given type on the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        /// <returns>Array of references to the components of the specified type</returns>
        public T[]? GetComponents<T>() where T : Component
        {
            Type targetType = typeof(T);
            var foundComponents = components.FindAll(c => targetType.IsAssignableFrom(c.GetType()));

            List<T>? convertedComponents = new List<T>();
            
            foreach(Component a in foundComponents)
            {
                // Use reflection to create an instance of the specified type and cast the found component to it.
                convertedComponents.Add((T)Convert.ChangeType(a, targetType));
            }
            return convertedComponents.Count != 0 ? convertedComponents.ToArray() : null;
        }

        #endregion
        /// <summary>
        /// Self destructs this GameObject and all its components
        /// </summary>
        public void DestroySelf()
        {
            foreach(Component c in components) { if(c.GetType() != typeof(Transform)){ DestroySelf(); } }
            Debug.LogInfo($"GameObject {name} should be destroyed. (remove all references to this object)");
        }
    }



    /// <summary>
    /// Converts Components into specific derivatives of Components during serialization of a component
    /// </summary>
    public class ComponentConverter : JsonConverter<Component> //diky chatgpt :3
    {
        public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                switch (jsonDoc.RootElement.GetProperty("Type").GetString())
                {
                    case nameof(Transform):
                        return jsonDoc.RootElement.Deserialize<Transform>(options) as Transform;
                    case nameof(Animator):
                        return jsonDoc.RootElement.Deserialize<Animator>(options) as Animator;
                    case nameof(Sprite):
                        return jsonDoc.RootElement.Deserialize<Sprite>(options) as Sprite;
                    case nameof(Tilemap):
                        return jsonDoc.RootElement.Deserialize<Tilemap>(options) as Tilemap;
                    case nameof(BoxCollider):
                        return jsonDoc.RootElement.Deserialize<BoxCollider>(options) as BoxCollider;
                    case nameof(UISprite):
                        return jsonDoc.RootElement.Deserialize<UISprite>(options) as UISprite;
                    default:
                        throw new JsonException("'Type' doesn't match a known derived type");
                }

            }
        }

        public override void Write(Utf8JsonWriter writer, Component value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
   
}

