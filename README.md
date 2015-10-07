# MandrillBackupNet

C# Console app to export and import mandrill templates to disk.  
  
MandrillBackupNet is a tool to save backups of Mandrill email templates to a local disk. 
Mandrill is great, but has no versioning of templates. If you delete something, it's gone forever! 
Regular backups are a safe guard against accidentally blowing away all of your templates.
  
See (Pre-existing GO version which uses AWS s3)[https://github.com/publicgoodsw/mandrill-backup]  
  
Options: Export, Import, Delete; complete account or by template name  
> Delete will always make a backup first..
  
## Usage

- Export from Mandril to disk
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Export`
- Import from Disk t0 Mandrill
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Import`
- Delete all templates in account (dont worry, we will make a backup first)
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a delete`

	
Import and exporting just a single template:  
- Export from Mandril to disk
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Export` -t "Your template name"
- Import from Disk t0 Mandrill
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a Import` -t "Your template name"
- Delete all templates in account (dont worry, we will make a backup first)
    - `mandrill-backup.exe -e c:\temp\mandrill -k <your-mandril-api-key>> -a delete` -t "Your template name"
  
Note:  
:unamused: *The template name in above command is really the template name and not the slug. It will find the correct template by listing all templates and comparing the names.
There is some strange API issue where all add/update/delete parameters to identify a specific template is called name, while this is actually the slug. The template has a name and slug field.*
	
	
    
## Todo

- Add import/export on single template name
