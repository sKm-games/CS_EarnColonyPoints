using ModLoaderInterfaces;

namespace CS_ColonyPoints
{
    [ModLoader.ModManager]
    public class CS_NewColony:
        IOnCreatedColony
    {
        private int _newColonyPoints;
        
        private ConfigReader _configReader;
        private CS_ModController _modController;
        
        /*
         * Initialize the new colony mod part
         * modController - the mod controller
         * configReader - the config reader
         */
        public void Initialize(CS_ModController modController, ConfigReader configReader)
        {
            _modController = modController;
            _configReader = configReader;
            _newColonyPoints = _configReader.GetInt("newColonyPoints");
            
            _modController.SendChatDebugNotification("MOD: EarnColonyPoint: NewColony: Initialize");
        }
        
        /**
         * Triggers when a new colony is created
         * colony - the colony that was created
         */
        public void OnCreatedColony(Colony colony)
        {
            _modController.SendChatDebugNotification("MOD: EarnColonyPoint: NewColony: OnCreatedColony");
            
            _modController.UpdateColonyPoints(colony.ColonyGroup, _newColonyPoints, _newColonyPoints);
        }
    }
}