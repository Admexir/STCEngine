﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;

namespace STCEngine
{
    /// <summary>
    /// Base for all components, components define the properties of GameObjects
    /// </summary>
    public abstract class Component
    {
        public bool enabled;
        /// <summary>
        /// the GameObject this component is attached to
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Function that gets called upon destroying this component or the GameObject its attached to
        /// </summary>
        public abstract void DestroySelf();
    }

    /// <summary>
    /// Component responsible for position, rotation and scale of the GameObject
    /// </summary>
    public class Transform : Component
    {
        public Vector2 position;
        public float rotation;
        public Vector2 size;

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
    }
    /// <summary>
    /// Component responsible for holding visual information about the GameObject
    /// </summary>
    public class Sprite : Component
    {
        public Image image;
        public Sprite(string fileSourceDirectory, GameObject gameObject)
        {
            this.image = Image.FromFile(fileSourceDirectory);
            EngineClass.AddSpriteToRender(gameObject);
        }

        public override void DestroySelf()
        {
            if (gameObject.GetComponent<Sprite>() != null) { gameObject.RemoveComponent<Sprite>(); return; }
            EngineClass.RemoveSpriteToRender(gameObject);
        }
    }

    public class Animator : Component
    {
        public Sprite? sprite;
        public Dictionary<string, Animation> animations { get; private set; }
        public float playBackSpeed;
        public static Animation? currentlyPlayingAnimation { get; private set; }
        public bool isPlaying { get; private set; }
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
            if (animations.TryGetValue(animationName, out Animation? animation)) { EngineClass.runningAnimations.Add(animation); animation.sprite = sprite; currentlyPlayingAnimation = animation; isPlaying = true; } 
            else { Debug.LogError($"Animation {animationName} not found and couldnt be played."); }     
        }
        public void Stop()
        {
            try
            {
                EngineClass.runningAnimations.Remove(currentlyPlayingAnimation??throw new Exception("Trying to stop animator that isnt playing!"));
                currentlyPlayingAnimation = null;
                isPlaying = false;
            }catch(Exception e) { Debug.LogError(e.Message); }
        }


        public override void DestroySelf()
        {
            if(gameObject.GetComponent<Animator>() != null) { gameObject.RemoveComponent<Animator>(); return; }
        }
    }
    public class AnimationFrame
    {
        public Image image;
        /// <summary>
        /// How long the frame stays in ms
        /// </summary>
        public int time;
        /// <summary>
        /// How long the frame stays in ms
        /// </summary>
        /// <param name="image"></param>
        /// <param name="time"></param>
        public AnimationFrame(Image image, int time)
        {
            this.image = image;
            this.time = time;
        }
    }
    public class Animation
    {
        public string name;
        public AnimationFrame[] animationFrames;
        private int timer, nextFrameTimer, animationFrame;
        public Sprite? sprite;

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
                else
                {
                    sprite.image = animationFrames[0].image;
                    nextFrameTimer = animationFrames[0].time;
                    timer = 0;
                    animationFrame = 0;
                }            }
            timer+=10;
        }

        public Animation(string name, AnimationFrame[] animationFrames)
        {
            this.name = name;
            this.animationFrames = animationFrames;
            timer = 0; nextFrameTimer = animationFrames[0].time; animationFrame = 0;
        }
    }

    /// <summary>
    /// Any object inside the game, has a name and a list of components that defines its properties
    /// </summary>
    public class GameObject
    {
        public List<Component> components = new List<Component>();
        public string name;
        /// <summary>
        /// Defines whether the object is currently active/enabled in the game, inactive GameObjects do not affect the game in any way
        /// </summary>
        public bool isActive;
        public Transform transform;

        #region Constructors
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
        /// Called upon creating a GameObject, registers the object in the Engine class and prints a debug
        /// </summary>
        #endregion
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
        /// <summary>
        /// Self destructs this GameObject and all its components
        /// </summary>
        public void DestroySelf()
        {
            foreach(Component c in components) { if(c.GetType() != typeof(Transform)){ DestroySelf(); } }
            Debug.LogInfo($"GameObject {name} should be destroyed. (remove all references to this object)");
        }

        #region Component Managment
        /// <summary>
        /// Adds the given component to the GameObject
        /// </summary>
        /// <param name="component"></param>
        /// <returns>The newly added componnent</returns>
        public Component AddComponent(Component component)
        {
            components.Add(component);
            component.gameObject = this;
            Debug.LogInfo($"Component {component} has been added onto GameObject {this.name}");
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
        #endregion
    }
}