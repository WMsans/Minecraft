using Unity.Jobs;

namespace sapra.InfiniteLands
{
    //Output final node that has the exact same resolution output as the settings define
    public interface IOutput
    {       
        public string OutputVariableName{ get; }
    }
}