using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.Shikaka.ChildActorContainers;
using Tumblr.Bot.SQLite;
using Tumblr.Bot.UserInterface;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class SupervisorPropsContainer
    {
        public SupervisorPropsContainer(
            int maxWorkers,
            Collections collections,
            ObservableCollection<DataGridItem> dataGridUiControlSourceCollection,
            GlobalStats globalStats,
            SQLiteDb sqLiteDatabase, 
            TextBox chatLogTextBox,
            ReadOnlyDictionary<string, Dictionary<string, ScriptWaifu>> convos,
            List<int> workersOnline)
        {
            MaxWorkers = maxWorkers;
            Collections = collections;
            DataGridUiControlSourceCollection = dataGridUiControlSourceCollection;
            GlobalStats = globalStats;
            SQLiteDatabase = sqLiteDatabase;
            ChatLogTextBox = chatLogTextBox;
            Convos = convos;
            WorkersOnline = workersOnline;
        }

        public int MaxWorkers { get; }
        public Collections Collections { get; }
        public ObservableCollection<DataGridItem> DataGridUiControlSourceCollection { get; }
        public GlobalStats GlobalStats { get; }
        public SQLiteDb SQLiteDatabase { get; }
        public TextBox ChatLogTextBox { get; }
        public ReadOnlyDictionary<string, Dictionary<string, ScriptWaifu>> Convos { get; }
        public List<int> WorkersOnline { get; }
        public SupervisorChildActorsContainer Children { get; set; }
    }
}
