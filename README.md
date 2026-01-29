# BackupSystem

System tworzący kopie zapasowe katalogów w czasie rzeczywistym.


## Uzycie

Użytkownik do dyspozyci następujące polecenia:

- add "source path" "target path" uruchamia tworzenie kopii folderu "source path" w miejscu wskazanym przez "target path"
- end "source path" "target paths" przerywa tworzenie kopii zapasowych
- list wypisuje jakie kopie są śledzone
- restore "source path" "target path" przywraca kopię

## Mechanizm działania

- BackupManager obsługuje zapytania użytkownika
- Każdej kopii odpowiada jeden obiekt BackupWorker
- Lista aktywnych kopii jest reprezentowana jako słownik (źródło, cel) -> BackupWorker
- BackupWorker monitoruje zmiany z użyciem FileSystemWatcher
- SyncEngine odpowiada za proces kopiowania plików
- Logger informuje użytkownika o zmianach i ewentualnych błędach
