using System;
using System.Collections.Generic;
using System.IO;
using Chatting;
using ModLoaderInterfaces;
using Monsters;
using Math = Pipliz.Math;

namespace CS_ColonyPoints
{
  [ModLoader.ModManager]
    public class CS_ModController :
    IAfterWorldLoad,
    IOnPlayerConnectedLate,
    IOnMonsterDied,
    IOnUpdate
    {
      
      //Holds the points for each colony
        public readonly Dictionary<ColonyGroupID, double> ColonyPoints = new Dictionary<ColonyGroupID, double>();
        
        private CS_ZombieKills _zombieKills;
        private CS_NewColony _newColony;
            
        //Holds the next time we are allowed to send a chat notification
        private double _nextNotification;
        
        private ConfigReader _configReader;
        private bool _debugText;
        private bool _pointsOverflow;
        private bool _chatNotifications;
        private int _rewardTrigger;
        
        /*
         * Triggers when the world is loaded
         */
        public void AfterWorldLoad()
        {
            
        }
        
        
        /*
         * Triggers when a player connects late
         */
        public void OnPlayerConnectedLate(Players.Player player)
        {
          //Set up mod info
          Initialize();
          
          Chat.Send(Players.ConnectedPlayers,"MOD: EarnColonyPoint: Initialized", (EChatSendOptions) 3);
          Chat.Send(Players.ConnectedPlayers,"MOD: EarnColonyPoint: Earn Colony Points by killing Zombies and creating new Colony", (EChatSendOptions) 3);
        }

        public void OnMonsterDied(IMonster monster)
        {
          _zombieKills.OnMonsterKill(monster);
        }
        
        /*
         * Runs each frame?
         */
        void IOnUpdate.OnUpdate()
        {
          if (!CheckAllowUpdate())
          {
            return;
          }

          foreach (ColonyGroupID colonyGroupID in ColonyPoints.Keys)
          {
            ColonyGroup colonyGroup = ServerManager.ColonyTracker.Get(colonyGroupID);
            SendChatDebugNotification(
              $"MOD: EarnColonyPoint: OnUpdate: CheckReward for colony {colonyGroup.Name} ");

            //Skip if colony is already at max Colony Points
            if (colonyGroup.ColonyPoints >= colonyGroup.ColonyPointsCap)
            {
              ColonyAtMaxPoints(colonyGroup);
            }

            double colonyPointsEarned = ColonyPoints[colonyGroupID];
            if (colonyPointsEarned >= 1.0)
            {
              //Calculate how many points we can give to the colony
              CalculateColonyPoints(colonyPointsEarned, colonyGroup, out double pointsReward, out double pointsLost);

              //Update the colony points and check for overflow
              UpdateColonyPoints(colonyGroup, colonyPointsEarned, pointsReward);

              //Send a chat notification for the colony
              SendChatRewardNotification(colonyGroup, pointsReward, pointsLost);
            }
          }
        }
        
        
        /**
         * Check if we are allowed to update the colony points
         */
        private bool CheckAllowUpdate()
        {
          //Check if we are rewarding each day
          if (_rewardTrigger == 1)
          {
            return false;
          }

          //Check if we are at the next notification time
          if (_nextNotification >= TimeCycle.TotalHours)
          {
            return false;
          }

          //Calculate the next notification time
          _nextNotification = TimeCycle.TotalHours + TimeCycle.TimeTillSunRise.Value.TotalHours;
          return true;
        }
        
        /*
         * Initialize the mod
         */
        private void Initialize()
        {
          string jsonPath = GetJsonPath();
          _configReader = new ConfigReader(jsonPath);
          
          _debugText = _configReader.GetBool("debugText");
          _chatNotifications = _configReader.GetBool("chatNotifications");
          _pointsOverflow = _configReader.GetBool("pointsOverflow");
          _rewardTrigger = _configReader.GetInt("rewardTrigger");
          
          _zombieKills = new CS_ZombieKills();
          _zombieKills.Initialize(this, _configReader);
          
          _newColony = new CS_NewColony();
          _newColony.Initialize(this, _configReader);
        }
        
        
        /*
         * Get json path
         */
        private string GetJsonPath()
        {
          SendChatDebugNotification("MOD: EarnColonyPoint: GetSettingsPath");
          string assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        
          if (assemblyLocation == null)
          {
            throw new Exception("Assembly path not found");
          }
        
          string path = Path.Combine(assemblyLocation, "config.json");
        
          if (path == null)
          {
            SendChatDebugNotification("MOD: EarnColonyPoint: Config.json file not found");
            throw new Exception("config.json file not found");
          }
        
          return path;
        }
        
      /**
       * Send a chat notification to the colony group that they are at max Colony Points and check for overflow
       * colonyGroup - Colony group we are sending the notification for
       */
      private void ColonyAtMaxPoints(ColonyGroup colonyGroup)
      {
        string chatNotification = $"{colonyGroup.Name} is at current Colony Points Max so no points given for killing zombies.";
        Chat.Send(colonyGroup.Owners, chatNotification, (EChatSendOptions) 3);
        
        //Updates the colony points, to check if we allow the points to overflow
        UpdateColonyPoints(colonyGroup, 0, 0);
      }
      
      /**
       * Calculate how many points we can give to the colony
       * colonyPoint - Points the colony has earned
       * colonyGroup - Colony group we are checking
       * pointsEarned - Points the rewarded to the colony after checking
       * pointsLost - Points that where lost since we are at max Colony Points
       */
      private void CalculateColonyPoints(double colonyPoint, ColonyGroup colonyGroup, out double pointsReward, out double pointsLost)
      {
        long tempPoints = Math.FloorToInt(colonyPoint);

        pointsReward = tempPoints;
        pointsLost = 0;
        
        //Check if we are above the ColonyPointsCap
        if (colonyGroup.ColonyPointsCap < colonyGroup.ColonyPoints + tempPoints)
        {
          pointsReward = colonyGroup.ColonyPointsCap - colonyGroup.ColonyPoints;
          
          //Calculate how many points we lost since we are at max Colony Points
          pointsLost = tempPoints - pointsReward;
        }
      }
      
      /**
       * Update the colony points
       * colonyGroup - Colony group we are updating
       * colonyPointEarned - Points the colony has earned
       * pointsReward - Points the rewarded to the colony after checking
       */
      public void UpdateColonyPoints(ColonyGroup colonyGroup, double colonyPointEarned, double pointsReward)
      {
        colonyGroup.AddColonyPoints((long) pointsReward);
        
        //If we allow the points to overflow, calculate how many points we have left
        if(_pointsOverflow)
        {
          ColonyPoints[colonyGroup.ColonyGroupID] = colonyPointEarned - (double) pointsReward; 
        }
        //If we don't allow the points to overflow, set the points to 0
        else
        {
          ColonyPoints[colonyGroup.ColonyGroupID] = 0;
        }
        
        ColonyPoints[colonyGroup.ColonyGroupID] = colonyPointEarned - (double) pointsReward; 
      }
      
      /**
       * Send a chat notification to the colony group
       * colonyGroup - Colony group we are sending the notification for
       * pointsReward - Points the colony has earned
       * pointsLost - Points that where lost since we are at max Colony Points
       */
      private void SendChatRewardNotification(ColonyGroup colonyGroup, double pointsReward, double pointsLost)
      {
        // if (_settingsInfo.ChatNotificationsSettings.BoolValue == 0)
        if(!_chatNotifications)
        {
          return;
        }

        string chatNotification = $"{colonyGroup.Name}, ";
        
        //Points was given
        if (pointsReward > 0)
        {
          chatNotification += $" has received {pointsReward} Colony Points for killing zombies.";
          //Add message if points was lost do to max Colony Points
          if (pointsLost > 0)
          {
            chatNotification+= $" But {pointsLost} points was lost since colony reached max Colony Points.";
          }
        }
        //No points was given, player was at max Colony Points
        else
        {
          chatNotification += "is at max Colony Points so no points given for killing zombies."; 
        }
        
        Chat.Send(colonyGroup.Owners, chatNotification, (EChatSendOptions) 3);
      }
      
      public void SendChatDebugNotification(string message)
      {
        if(!_debugText)
        {
          return;
        }

        Chat.Send(Players.ConnectedPlayers,"DEBUG: " + message, (EChatSendOptions) 3);
      }
    }
}