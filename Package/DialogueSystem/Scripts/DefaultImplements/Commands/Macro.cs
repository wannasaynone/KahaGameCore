namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class Macro : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MacroFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new Macro();
        }
    }
}
