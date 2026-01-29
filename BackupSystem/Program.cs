using System;


namespace BackupSystem;

class Program
{
    private static readonly BackupManager _manager = new();
    private static bool _running = true;

    static void Main(string[] args)
    {
      
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; 
            
            Console.WriteLine(); 
            Logger.Info("Otrzymano sygnał przerwania. Zamykanie...");
            
       
            _manager.Shutdown();
            
            Logger.Info("Koniec pracy programu.");
         
            Environment.Exit(0);
        };

        Logger.Info("System Zarządzania Kopiami Zapasowymi");
        Logger.Info("Dostępne komendy: add, list, end, restore, exit");

       
        while (_running)
        {
            Console.Write("> ");
            
            
            string? line = Console.ReadLine();
            
            if (line == null) break; 

            var commandArgs = InputParser.ParseCommand(line);
            if (commandArgs.Length == 0) continue;

            string cmd = commandArgs[0].ToLower();

            try
            {
                switch (cmd)
                {
                    case "add":
                        if (commandArgs.Length < 3)
                            Logger.Error("Użycie: add <source> <target> [target2 ...]");
                        else
                            _ = _manager.AddBackup(commandArgs[1], commandArgs.Skip(2).ToArray());
                        break;

                    case "end":
                        if (commandArgs.Length < 3)
                            Logger.Error("Użycie: end <source> <target> [target2 ...]");
                        else
                            _manager.EndBackup(commandArgs[1], commandArgs.Skip(2).ToArray());
                        break;

                    case "list":
                        _manager.ListBackups();
                        break;

                    case "restore":
                        if (commandArgs.Length != 3)
                            Logger.Error("Użycie: restore <source> <target>");
                        else
                            _manager.RestoreBackup(commandArgs[1], commandArgs[2]);
                        break;

                    case "exit":
                        _running = false;
                        break;

                    default:
                        Logger.Error($"Nieznana komenda: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Wystąpił nieoczekiwany błąd: {ex.Message}");
            }
        }
        
        _manager.Shutdown();
        Logger.Info("Koniec pracy programu.");
    }
}