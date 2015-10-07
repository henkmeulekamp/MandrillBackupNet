# MandrillBackupNet

C# Console app to export and import mandrill templates to disk
  
Usage:
- Export from Mandril to disk
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Export`
- Import from Disk t0 Mandrill
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Import`
- Delete all templates in account (dont worry, we will make a backup first)
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a delete`

    
## Todo

- Add import/export on single template name
