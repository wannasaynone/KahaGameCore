using System.Collections.Generic;

namespace KahaGameCore.Package.ActorSystem.Definition
{
    public class ActorCollection
    {
        public static ActorCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ActorCollection();
                }

                return instance;
            }
        }
        private static ActorCollection instance;

        private ActorCollection() { }

        private readonly List<Actor> actors = new List<Actor>();

        public void Bind(Instance instance, ControllerBase controller)
        {
            Actor actor = actors.Find(a => a.Instance == instance);
            if (actor == null)
            {
                actor = new Actor();
                actor.UpdateInstance(instance);
                actors.Add(actor);
            }
            actor.AddController(controller);
        }

        public void Bind(Instance instance, List<ControllerBase> controllers)
        {
            Actor actor = actors.Find(a => a.Instance == instance);
            if (actor == null)
            {
                actor = new Actor();
                actor.UpdateInstance(instance);
                actors.Add(actor);
            }
            foreach (var controller in controllers)
            {
                actor.AddController(controller);
            }
        }

        public void Unbind(Instance instance, ControllerBase controller)
        {
            Actor actor = actors.Find(a => a.Instance == instance);
            if (actor != null)
            {
                actor.RemoveController(controller);
            }
        }
    }
}