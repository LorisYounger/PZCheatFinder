namespace PZCheatFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("僵毁等级挂检测工具 by LorisYounger");
            Console.WriteLine("github: https://github.com/LorisYounger/PZCheatFinder");

            Console.WriteLine("请输入 PerkLog 文件路径 eg: F:\\Zomboid\\log\\10-01-24_03-54-36_PerkLog.txt");
            var path = Console.ReadLine().Trim('"');
            List<Data> data = File.ReadAllLines(path).Select(Data.Parse).ToList();
            AnalyzeData(data);
            Console.WriteLine($"分析完成, 总计数据:{data.Count} 若为空结果则说明没有开等级挂的, 并不代表其他情况");
            Console.WriteLine("若分析出疑似开挂的人, 也有可能是管理员给与的等级或者奖励或者误差, 还请在日志中复查");
            Console.WriteLine();
            Console.WriteLine("AD:欢迎来玩 虚拟桌宠模拟器/虚拟主播模拟器, 现已上线Steam");
            Console.WriteLine("AD:杨远洛里斯服务器 QQ:929180788");
            Console.WriteLine("按任意键退出");
            Console.ReadKey();
        }

        public static void AnalyzeData(List<Data> dataList)
        {
            // 使用字典存储每个玩家的等级变化和上次等级变化的时间
            Dictionary<string, Dictionary<string, (int Level, int Hour)>> playerLevels = new Dictionary<string, Dictionary<string, (int Level, int Hour)>>();
            // 存储开挂的玩家和触发次数
            Dictionary<(string Id, string Name), int> cheatingPlayers = new Dictionary<(string Id, string Name), int>();
            foreach (var data in dataList)
            {
                // 跳过初始化数据
                if (Data.IsEvent(data.DataType))
                    continue;

                if (!playerLevels.ContainsKey(data.Id))
                {
                    playerLevels[data.Id] = new Dictionary<string, (int Level, int Hour)>();
                }
                if (data.DataType == "Initial Data")
                {
                    foreach (var kvp in data.InitialData)
                    {
                        playerLevels[data.Id][kvp.Key] = (kvp.Value, data.HoursSurvived);
                    }
                }
                else if (data.DataType == "Level Changed")
                {
                    var prevData = playerLevels[data.Id].ContainsKey(data.LevelChangeType) ? playerLevels[data.Id][data.LevelChangeType] : (0, data.HoursSurvived);
                    if (data.LevelChangeValue - prevData.Item1 > 2 && data.HoursSurvived - prevData.Item2 <= 1)
                    {
                        Console.WriteLine($"警告: 玩家 {data.Name}({data.Id}) 在1小时内 {data.LevelChangeType} 等级变化超过2级! 详细:{data}");
                        var playerKey = (data.Id, data.Name);
                        if (cheatingPlayers.ContainsKey(playerKey))
                        {
                            cheatingPlayers[playerKey]++;
                        }
                        else
                        {
                            cheatingPlayers[playerKey] = 1;
                        }
                    }
                    if (data.LevelChangeType == "LongBlade" && data.LevelChangeValue > 9 && data.HoursSurvived < 300)
                    {
                        Console.WriteLine($"警告: 玩家 {data.Name}({data.Id}) 在300小时内 长刀 等级超过9级! 详细:{data}");
                        var playerKey = (data.Id, data.Name);
                        if (cheatingPlayers.ContainsKey(playerKey))
                        {
                            cheatingPlayers[playerKey]++;
                        }
                        else
                        {
                            cheatingPlayers[playerKey] = 1;
                        }
                    }

                    if (prevData.Item2 != data.HoursSurvived || data.LevelChangeValue < 4)
                    {
                        playerLevels[data.Id][data.LevelChangeType] = (data.LevelChangeValue, data.HoursSurvived);
                    }
                }
            }
            // 输出统计结果
            Console.WriteLine("\n开挂玩家统计：");
            foreach (var kvp in cheatingPlayers)
            {
                Console.WriteLine($"玩家 ID: {kvp.Key.Id}, 名字: {kvp.Key.Name}, 开挂次数: {kvp.Value}");
            }
            Console.WriteLine("\n封禁代码自动生成(作弊超过20次):");
            foreach (var kvp in cheatingPlayers)
            {
                if (kvp.Value > 20)
                {
                    Console.WriteLine($"banid {kvp.Key.Id}");
                }
            }
        }
    }
    public class Data
    {
        public DateTime Date { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string DataType { get; set; }
        public Dictionary<string, int> InitialData { get; set; }
        public string LevelChangeType { get; set; }
        public int LevelChangeValue { get; set; }
        public int HoursSurvived { get; set; }

        public static bool IsEvent(string input)
        {
            return input.StartsWith("Created Player") || input == "Login" || input == "Died";
        }
        public static Data Parse(string input)
        {
            var parts = input.Split(new[] { '[', ']' }).ToList().FindAll(x => !string.IsNullOrWhiteSpace(x) && x != ".");
            Data data = new Data();
            data.Date = DateTime.ParseExact(parts[0], "yy-MM-dd HH:mm:ss.fff", null);
            data.Id = parts[1];
            data.Name = parts[2];
            data.Position = parts[3];
            data.HoursSurvived = int.Parse(parts[parts.Count - 1].Split(':')[1]);


            if (IsEvent(parts[4]))
            {
                data.DataType = parts[4];
            }
            else if (parts[4] == "Level Changed" && parts.Count > 6)
            {
                data.DataType = "Level Changed";
                data.LevelChangeType = parts[5];
                data.LevelChangeValue = int.Parse(parts[6]);
            }
            else if (parts.Count > 4)
            {
                data.DataType = "Initial Data";
                data.InitialData = ParseInitialData(parts[4]);
            }

            return data;
        }

        private static Dictionary<string, int> ParseInitialData(string initialData)
        {
            var parts = initialData.Replace(", ", ",").Split(',');
            var data = new Dictionary<string, int>();
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                data[keyValue[0]] = int.Parse(keyValue[1]);
            }

            return data;
        }
        public override string ToString()
        {
            string initialData = InitialData != null ? string.Join(", ", InitialData.Select(kvp => $"{kvp.Key}={kvp.Value}")) : string.Empty;

            switch (DataType)
            {
                case "Created Player":
                    return $"日期: {Date}, ID: {Id}, 名称: {Name}, 位置: {Position}, 类型: 创建用户1, 生存小时: {HoursSurvived}";
                case "Initial Data":
                    return $"日期: {Date}, ID: {Id}, 名称: {Name}, 位置: {Position}, 类型: 初始化数据, 数据: {initialData}, 生存小时: {HoursSurvived}";
                case "Level Changed":
                    return $"日期: {Date}, ID: {Id}, 名称: {Name}, 位置: {Position}, 类型: 等级变化, 数据类型: {LevelChangeType}, 变更变化: {LevelChangeValue}, 生存小时: {HoursSurvived}";
                default:
                    if (DataType.StartsWith("Created Player"))
                        return $"日期: {Date}, ID: {Id}, 名称: {Name}, 位置: {Position}, 类型: 创建用户{DataType}, 生存小时: {HoursSurvived}";
                    return $"日期: {Date}, ID: {Id}, 名称: {Name}, 位置: {Position}, 类型: {DataType}, 生存小时: {HoursSurvived}";
            }
        }
    }


}
