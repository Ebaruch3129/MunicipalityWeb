# עיר בקשר

ה־Solution כולל ארבעה פרויקטים:

- `Web` — אתר Blazor בעברית, כולל פתיחת פניות ומעקב אחריהן.
- `Model` — מחלקות הנתונים `BaseEntity` ו־`User`.
- `DAL` — אתחול מסד הנתונים ושאילתות עבור משתמשים ופניות.
- `API` — פעולות HTTP עבור משתמשים ופניות.

## מסד הנתונים

יש להפעיל שרת MySQL ולהגדיר את פרטי החיבור ב־User Secrets של פרויקט ה־API. מחרוזת החיבור צריכה להתחבר לשרת בלבד, בלי לציין שם של מסד נתונים; האתחול יוצר את `MunicipalityDB` ואת הטבלאות.

בעת הפעלת ה־API, האתחול יוצר את `MunicipalityDB` ואת הטבלאות `Roles`,‏ `Users`,‏ `Categories`,‏ `Reports`,‏ `ReportImages`,‏ `ReportStatusHistory`,‏ `ReportComments` ו־`UserSessions`.

דיווח חדש נשמר ב־`Reports`; תמונה מצורפת נשמרת ב־`ReportImages`. משתמש יכול להחזיק עד שתי פניות פעילות במקביל. סגירת פנייה מעדכנת את הסטטוס ואת `ReportStatusHistory`, והעדכון מופיע באזור האישי של המשתמש.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;User ID=YOUR_USER;Password=YOUR_PASSWORD;" --project API/API.csproj
```

## הפעלה מקומית

יש להפעיל את ה־API ואת האתר יחד. ב־Visual Studio בוחרים בפרופיל הפתרון `API and Web`. לחלופין, פותחים שני טרמינלים בתיקיית הפתרון:

```powershell
dotnet run --project API/API.csproj --launch-profile http
```

```powershell
dotnet run --project Web.csproj
```

האתר פונה ל־API ב־`http://localhost:5233/`. יש להגדיר את `Api:BaseUrl` אם כתובת ה־API שונה.

פרטי מנהל המערכת מוגדרים ב־User Secrets של פרויקט ה־API תחת `Admin:Email` ו־`Admin:Password`; אין לשמור סיסמאות בקובצי המקור.
