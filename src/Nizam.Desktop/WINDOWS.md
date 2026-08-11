# Nizam.Desktop — Windows WPF başlığı (gelecek)

Bu klasör şu an **net8.0 sınıf kitaplığıdır**. macOS ve CI üzerinde ViewModel, servis ve Gantt yerleşim mantığı derlenir ve test edilir.

## Neden WPF değil?

`UseWPF` / `net8.0-windows` macOS’ta derlenmez. Bu yüzden:

- ViewModels, Services, Models, Controls → **net8.0** (bu proje)
- `Views/*.xaml` → **Content / belgeleme** (derlenmez; Windows UI iskeleti)

## Windows’ta WPF head ekleme (plan)

1. Yeni proje oluşturun: `src/Nizam.Desktop.Wpf/Nizam.Desktop.Wpf.csproj`
2. Hedef: `net8.0-windows`, `<UseWPF>true</UseWPF>`, `<OutputType>WinExe</OutputType>`
3. Bu kitaplığa proje referansı ekleyin (`Nizam.Desktop`)
4. `Views/*.xaml` dosyalarını WPF projesine taşıyın veya bağlantılı dosya olarak ekleyin (`Page` / `ApplicationDefinition`)
5. `App.xaml` içinde `Host.CreateDefaultBuilder` + `AddNizamDesktop(new Uri("http://localhost:5169"))`
6. Code-behind’lerde `DataContext` olarak ViewModel’leri çözün

Örnek csproj parçası:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Nizam.Desktop\Nizam.Desktop.csproj" />
  </ItemGroup>
</Project>
```

## Gantt performansı

`Controls/GanttLayoutEngine.cs` / `VirtualizedGanttLayout` yalnızca görünür satır ve zaman penceresi için dikdörtgen üretir. WPF tarafında canvas/DrawingVisual bu dikdörtgenleri çizer; bar başına UserControl oluşturmayın.
