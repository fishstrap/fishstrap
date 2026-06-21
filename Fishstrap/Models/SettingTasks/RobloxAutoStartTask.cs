namespace Fishstrap.Models.SettingTasks
{
    public class RobloxAutoStartTask : BoolBaseTask
    {
        public RobloxAutoStartTask() : base("RobloxAutoStart") { }

        public override void Execute()
        {
            if (NewState)
                WindowsRegistry.DisableRobloxAutoStart();
            else
                WindowsRegistry.EnableRobloxAutoStart();

            OriginalState = NewState;
        }
    }
}
