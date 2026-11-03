# עיר בקשר

פרויקט ה־Solution מכיל ארבעה פרויקטים, כולם בתוך הריפו:

- `Web` — דפי התצוגה בעברית ובכיוון RTL. דפי הכניסה וההרשמה הם עיצוב בלבד ואינם שולחים נתונים.
- `API` — שירות Web API ו־`UsersController`.
- `DAL` — גישה לנתונים ויצירת בסיס הנתונים והטבלאות.
- `Model` — הישויות `BaseEntity` ו־`User`.

## הגדרת מסד הנתונים

פרויקט `API` משתמש ב־User Secrets עבור מחרוזת החיבור. אין להוסיף את המחרוזת לקובצי `appsettings.json` או לריפו.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;User ID=YOUR_USER;Password=YOUR_PASSWORD;" --project API/API.csproj
```

בהפעלה, `DatabaseInitializer` יוצר את `MunicipalityDB`, את שבע הטבלאות ואת תפקידי ברירת המחדל.

## הרצה

1. מפעילים MySQL ומגדירים את מחרוזת החיבור ב־User Secrets.
2. מפעילים את ה־API: `dotnet run --project API/API.csproj --launch-profile http`.
3. מפעילים את אתר התצוגה: `dotnet run --project Web.csproj`.
4. הדפים זמינים ב־`/`,‏ `/signin` ו־`/register`.

קובץ `API/API.http` כולל בקשות לדוגמה לכל פעולות ה־API.
