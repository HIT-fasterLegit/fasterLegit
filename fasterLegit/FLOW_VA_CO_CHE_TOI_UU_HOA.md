# Flow và cơ chế tối ưu của VELORA

Tài liệu này mô tả code đang có trong working tree hiện tại, không mô tả các tính năng chỉ còn
resource dịch hoặc đã bị xóa khỏi source. Mục tiêu là trả lời hai câu hỏi:

1. Khi người dùng bật hoặc tắt một mục trong trang **Optimize**, lệnh đi qua những lớp nào?
2. Vì sao ứng dụng có thể thay đổi cài đặt Windows, và mỗi mục tối ưu thực sự ghi gì vào máy?

> Phạm vi hiện tại: bản Release có 4 nhóm, 9 mục tối ưu. Cả 9 mục đều thay đổi Windows Registry.
> Nhóm `Debug` chỉ tồn tại trong bản Debug, dùng để thử flow thành công/thất bại/retry và không
> thay đổi máy.

## 1. Bức tranh tổng thể

fasterLegit là ứng dụng WPF chỉ chạy trên Windows. Phần Optimize không gọi một script tổng hợp
và cũng không có nhiều engine độc lập. Mọi lần thay đổi hệ thống đều đi qua một đường chạy chung:

```text
Người dùng gạt công tắc trên OptimizePage
    ↓
OptimizationCategoryViewModel.ToggleOptimizationCommand
    ↓
Kiểm tra file revert để quyết định Apply hay Revert
    ↓
Hỏi tạo Restore Point (lần đầu trong phiên)
    ↓
OptimizationService
    ↓
OperationRunner tạo OptimizationContext + ChangeSet
    ↓
Optimization.ApplyAsync của mục được chọn
    ↓
Provider sở hữu thao tác Windows, hiện tại là RegistryService
    ↓
Microsoft.Win32.RegistryKey đọc trạng thái cũ, so sánh, rồi ghi trạng thái mới
    ↓
ChangeSet ghi lại Change / Skip / lỗi + dữ liệu hoàn tác
    ↓
OperationRunner phân loại kết quả, lưu Revert JSON và History JSON
    ↓
ViewModel cập nhật thẻ, hiện thông báo hoặc hộp thoại retry
```

Các file trung tâm:

- UI: [`UI/Pages/Optimize/OptimizePage.xaml`](UI/Pages/Optimize/OptimizePage.xaml)
- Điều phối UI: [`UI/ViewModels/Optimizer/OptimizationCategoryViewModel.cs`](UI/ViewModels/Optimizer/OptimizationCategoryViewModel.cs)
- Danh sách mục tối ưu: [`Domain/Optimizations/Categories/`](Domain/Optimizations/Categories/)
- Service apply/revert: [`Services/Optimization/OptimizationService.cs`](Services/Optimization/OptimizationService.cs)
- Vòng đời dùng chung: [`Services/Optimization/OperationRunner.cs`](Services/Optimization/OperationRunner.cs)
- Provider Registry: [`Services/System/Primitives/RegistryService.cs`](Services/System/Primitives/RegistryService.cs)
- Lưu và chạy hoàn tác: [`Services/Revert/RevertManager.cs`](Services/Revert/RevertManager.cs)

## 2. Vì sao ứng dụng có thể “chọc vào” Windows?

Có ba điều kết hợp với nhau.

### 2.1 Ứng dụng yêu cầu quyền Administrator ngay từ lúc mở

[`app.manifest`](app.manifest) chứa:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>
```

Windows vì vậy hiển thị UAC trước khi chạy. Nếu người dùng không chấp nhận, ứng dụng không khởi
động. Khi đã chấp nhận, process có access token đã nâng quyền, nên có thể ghi phần lớn khóa
`HKEY_LOCAL_MACHINE` (`HKLM`) và thực hiện các thao tác hệ thống cần quyền quản trị.

Quyền Administrator không có nghĩa là bỏ qua mọi bảo vệ. Khóa Registry được Windows bảo vệ riêng
vẫn có thể trả về `Access denied`; provider phải ghi nhận đây là lỗi thay vì giả vờ thành công.

### 2.2 Code gọi API Windows thật, không chỉ đổi trạng thái giao diện

`RegistryService` dùng `Microsoft.Win32.RegistryKey`:

- `OpenSubKey(..., writable: true)` để mở khóa có quyền ghi;
- `CreateSubKey(...)` nếu đường dẫn chưa tồn tại;
- `GetValue(...)` và `GetValueKind(...)` để sao lưu trạng thái trước đó;
- `SetValue(...)` để ghi `DWORD`, `String`, `QWORD`, `Binary` hoặc kiểu Registry tương ứng;
- `DeleteValue(...)` khi hoàn tác một giá trị vốn không tồn tại trước đó.

Do đó công tắc trong UI chỉ là điểm bắt đầu. Thao tác thực tế xảy ra khi `RegistryKey.SetValue`
được gọi trong process đang chạy với quyền đã nâng.

### 2.3 Windows và các thành phần của Windows đọc những khóa đó

Ứng dụng không tự tạo hiệu ứng như ẩn Recommended hoặc tắt quảng cáo. Nó ghi các giá trị mà
Explorer, Start, Settings, Content Delivery Manager hoặc hệ thống policy của Windows đọc. Khi các
thành phần đó nạp lại cấu hình, Windows tự thay đổi hành vi.

Vì vậy có hai thời điểm khác nhau:

- **Ghi thành công:** Registry đã chứa giá trị mới.
- **Hiệu ứng nhìn thấy:** có thể xảy ra ngay, sau khi Explorer/ứng dụng liên quan khởi động lại,
  sau sign-out/sign-in hoặc sau restart, tùy thành phần Windows và phiên bản Windows.

Các khóa dưới `...\Policies\...` mang ý nghĩa policy. Khóa thường dưới `CurrentVersion` hoặc
`Control Panel` là preference/cấu hình của user hoặc máy. Một bản Windows không còn hỗ trợ một
policy có thể vẫn cho ghi Registry nhưng bỏ qua giá trị đó.

## 3. Flow lúc ứng dụng khởi động

### Bước 1: dựng dependency graph

`App.xaml.cs` tạo Host và gọi
`ApplicationServiceCollectionExtensions.AddOptimizerApplication(...)`. Các thành phần liên quan
được đăng ký singleton:

- `OptimizePage` và `OptimizationCategoryViewModel`;
- `OptimizationRegistry`;
- `OptimizationService`;
- `OperationRunner`;
- `RevertManager`;
- `ShellService`, `PowerPlanService`, `SystemInfoService`, `StreamService`.

Host bật kiểm tra DI khi build, nên dependency thiếu hoặc lifetime sai sẽ lỗi từ startup thay vì
đợi đến lúc người dùng bấm Apply.

### Bước 2: dọn trạng thái và preload

Startup gọi `WmiHelper.Initialize()`, hiển thị `MainWindow`, rồi gọi
`OptimizationRegistry.PreloadOptimizationsAsync()`.

Registry không dùng mảng đăng ký viết tay. Nó dùng reflection:

1. `ReflectionHelper` tìm mọi class triển khai `IOptimizationCategory` trong assembly có tên bắt
   đầu bằng `fasterLegit`.
2. `CategoryDiscovery` tìm public nested class triển khai `IOptimization` trong từng category.
3. Mỗi item được tạo bằng constructor không tham số.
4. `OwnerType` được gán để tạo localization key.
5. `OptimizationValidation` kiểm tra GUID và ID trùng.
6. `ConditionValidation` kiểm tra condition khai báo đúng kiểu.
7. Category rỗng bị bỏ qua; category còn lại được sắp theo `OptimizationCategoryOrder`.

Sau discovery, `OptimizationService.UpdateOptimizationStateAsync()` kiểm tra file revert của từng
GUID. Có file revert nghĩa là item được xem là đang Applied. Đây là source of truth, không phải giá
trị hiện tại trong Registry.

### Bước 3: trang Optimize nạp dữ liệu

Khi ViewModel được khởi tạo lần đầu:

1. Chờ `OptimizationRegistry.EnsurePreloadedAsync()`.
2. Gom item của mọi category vào `_allOptimizations`.
3. Lấy `SystemInfo` snapshot.
4. Đánh giá condition, ví dụ `Windows11Condition`.
5. Áp dụng search, risk filter, hide-applied và sort.
6. Đưa danh sách vào `Optimizations`, XAML bind thành các card.

Condition `Unsupported` chỉ chặn bằng giao diện. Người dùng có thể chọn ẩn cảnh báo trong phiên để
hiện lại card thường. Condition lỗi hoặc snapshot không xác định sẽ fail-open, tức không tự ý ẩn
item.

## 4. Flow Apply: từ thao tác UI đến Registry

### 4.1 UI phát lệnh

Trong `OptimizePage.xaml`, `ToggleSwitch` bind:

```text
Command          → ToggleOptimizationCommand
CommandParameter → IOptimization của card
IsChecked        ↔ optimization.State.IsApplied
```

CommunityToolkit.Mvvm sinh `ToggleOptimizationCommand` từ method có `[RelayCommand]` trong
`OptimizationCategoryViewModel`.

### 4.2 ViewModel quyết định Apply hay Revert

`ToggleOptimizationAsync` không tin hoàn toàn vào trạng thái vừa bind trên UI. Nó gọi
`RevertManager.IsAppliedAsync(optimization.Id)` để kiểm tra file:

```text
%LocalAppData%\fasterLegit\Revert\{optimization-id}.json
```

- Không có file: gọi nhánh Apply.
- Có file hoặc có bản `.unreadable-*`: gọi nhánh Revert.

Cách này tránh trường hợp công tắc UI và dữ liệu hoàn tác trên disk lệch nhau.

### 4.3 Restore Point

Trước thay đổi đầu tiên trong phiên chạy app, ViewModel hiển thị dialog:

- Chấp nhận: `OptimizationService.CreateRestorePointAsync()` gọi `SystemRestoreService`.
- Skip: tiếp tục mà không có restore point.
- Cancel: dừng và trả công tắc về trạng thái cũ.

Nếu System Protection đang tắt, service thử bật cho system drive rồi tạo lại. Nếu tạo thất bại,
người dùng vẫn phải chọn tiếp tục không có restore point hoặc hủy. Cờ
`WasRequestedRestorePoint` làm cho câu hỏi này chỉ xuất hiện một lần trong phiên sau khi người dùng
đã chọn tiếp tục.

Restore Point là lớp an toàn cấp Windows. Nó độc lập với revert JSON chi tiết của từng item.

### 4.4 OptimizationService đóng gói yêu cầu

`OptimizationService.ApplyAsync()` tạo `OperationRequest` gồm:

- `OperationSubject`: GUID, key ổn định và tên tiếng Anh để log;
- tên hiển thị đã localization;
- logger riêng của loại optimization;
- `RevertPersistence.Enabled`;
- delegate trỏ tới `optimization.ApplyAsync`.

Sau đó nó chuyển request cho `OperationRunner.RunAsync()`.

### 4.5 OperationRunner tạo execution context

Runner tạo một `ChangeSet` mới rồi dựng `OptimizationContext` chứa:

- `Changes`: nơi provider ghi mọi step;
- `Logger`;
- `CancellationToken`;
- `Snapshot`: thông tin OS/phần cứng;
- `StreamService`;
- `ShellService`;
- `PowerPlanService`.

Context được truyền thẳng vào `ApplyAsync` của item. Không có global mutable context và không có
đường apply thứ hai.

### 4.6 Item gọi provider sở hữu thao tác

Các item hiện tại đều gọi:

```csharp
RegistryService.Write(context, new RegistryItem(path, name, value), ...);
```

Khi nhận nhiều `RegistryItem`, overload của `RegistryService.Write` vẫn thử **tất cả** item và ghi
một step riêng cho từng giá trị. Kết quả tổng trả về failure đầu tiên, nhưng các giá trị sau không
bị bỏ qua chỉ vì giá trị trước lỗi.

Với từng giá trị, provider làm theo thứ tự:

1. Parse root `HKCU`, `HKLM`, `HKEY_CURRENT_USER`, `HKEY_LOCAL_MACHINE`, v.v.
2. Mở khóa để đọc trạng thái cũ.
3. Nếu khóa thiếu, tạo từng đoạn subkey và nhớ chính xác subkey nào do lần chạy này tạo.
4. Đọc value cũ và `RegistryValueKind` cũ.
5. Nếu value, type và dữ liệu đã đúng, không gọi `SetValue`; ghi một `Skip`.
6. Nếu khác, gọi `SetValue`.
7. Tạo `RegistryRevertStep` chứa value cũ, type cũ và các subkey vừa tạo.
8. Ghi một `Change` thành công vào `ChangeSet`.
9. Nếu lỗi, ghi failed `Change`, error detail và retry delegate khi retry có thể hữu ích.

Điểm quan trọng: item chỉ yêu cầu trạng thái mong muốn. `RegistryService` chịu trách nhiệm đọc
trước, chống ghi thừa, backup, retry và ghi nhận step. Nhờ vậy không có 9 bản sao logic Registry.

### 4.7 Runner phân loại kết quả

`ChangeSet` phân biệt:

| Kind | Ý nghĩa | Đã sửa máy? | Cần revert? |
|---|---|---:|---:|
| `Change` | Đã thay đổi giá trị | Có | Có |
| `Skip` | Máy đã đúng sẵn | Không | Không |
| `NotApplicable` | Máy không có target phù hợp | Không | Không |
| `Refused` | Windows từ chối nhưng không coi là exception của cả run | Không | Không |
| `Irreversible` | Đã đổi nhưng bản chất không thể hoàn tác | Có | Không |

Runner suy ra kết quả cuối:

- `Success`: có thay đổi và không có step lỗi;
- `PartialSuccess`: đã thay đổi một phần nhưng còn step lỗi;
- `Failed`: không thay đổi được gì hoặc provider/runner lỗi hoàn toàn;
- `NothingToDo`: không có thay đổi, thường vì mọi value đã đúng và đều là `Skip`.

Nếu provider throw một lỗi không thể xử lý, runner biến exception thành failed step có tên người
dùng. Provider bình thường không nên vừa ghi lỗi rồi lại throw.

### 4.8 Lưu revert và history

Nếu có `Change` mang compensation, runner gọi `RevertManager.SaveRevertDataAsync()` trước khi trả
kết quả. File được ghi vào:

```text
%LocalAppData%\fasterLegit\Revert\{id}.json
```

Mỗi file chứa GUID, key, thời điểm apply và danh sách `RegistryRevertStep`. Ghi file theo kiểu
atomic: ghi `.tmp`, flush xuống disk, rồi `File.Replace`/`File.Move`. Có lock riêng theo GUID với
timeout 30 giây. Nếu app bị cancel giữa chừng, phần đã thay đổi vẫn được persist với
`CancellationToken.None` để không mất đường hoàn tác.

Runner cũng ghi báo cáo best-effort vào:

```text
%LocalAppData%\fasterLegit\History\{id}.json
```

History chứa step, before/after, thời gian, số lần retry, kết quả, phiên bản app và Windows. History
không được dùng để revert và vẫn tồn tại sau khi revert.

### 4.9 UI nhận kết quả

ViewModel:

- mở dialog chi tiết nếu có failed step có thể retry;
- khi retry thành công, append compensation mới vào revert file;
- cập nhật lại `IsApplied` từ file trên disk;
- đánh dấu `IsAlreadyOptimal` cho `NothingToDo` chỉ trong phiên hiện tại;
- hiện snackbar Success, Partial, Failed hoặc Nothing to do;
- đặt `App.HasPendingChanges = true` khi hệ thống thực sự có thay đổi.

## 5. Flow Revert: khi người dùng gạt công tắc về Off

Nếu file revert tồn tại, cùng `ToggleOptimizationCommand` đi vào nhánh Revert:

```text
OptimizationCategoryViewModel.RevertOptimizationAsync
    ↓
OptimizationService.RevertAsync
    ↓
RevertManager.RevertAsync
    ↓
Load Revert/{id}.json
    ↓
Deserialize từng IRevertStep
    ↓
Chạy theo thứ tự index giảm dần (LIFO)
    ↓
RegistryRevertStep gọi lại RegistryService
    ↓
Đọc lại Registry để xác minh
```

LIFO quan trọng khi một item được apply/retry nhiều lần: thay đổi mới nhất được tháo trước, cuối
cùng mới trở về trạng thái ban đầu thực sự.

Với Registry, revert có hai trường hợp chính:

- Value đã tồn tại trước Apply: ghi lại **đúng value và đúng RegistryValueKind cũ**.
- Value chưa tồn tại trước Apply: xóa value; sau đó chỉ xóa những subkey do app tạo và chỉ khi
  chúng vẫn hoàn toàn rỗng.

`RegistryRevertStep` không tin việc gọi API là đủ. Nó đọc lại để xác minh value đã bằng dữ liệu cũ
hoặc đã biến mất. Access denied hay read-back sai đều là failure.

Nếu tất cả step thành công, revert file bị xóa nên item trở về trạng thái chưa Applied. Nếu chỉ một
phần thành công, file được ghi lại chỉ còn các step thất bại; UI có thể cho retry. Khi hoàn tất,
History được đánh dấu là `Revert` và before/after của các thay đổi được đảo lại để phản ánh trạng
thái mới.

## 6. Từng mục tối ưu hiện có

### 6.1 Performance

Source: [`Domain/Optimizations/Categories/Performance.cs`](Domain/Optimizations/Categories/Performance.cs)

#### DisableAccessibilityKeyboardHotkeys

- ID: `B7BB32F8-C756-47A4-83F2-F6E7EC7D45B8`
- Risk: `Moderate`
- Mục đích: ngăn phím tắt Sticky Keys, Filter Keys và Toggle Keys bật popup ngoài ý muốn khi chơi
  game hoặc gõ nhanh.

| Hive/path | Value | Type suy ra | Giá trị mới |
|---|---|---|---:|
| `HKCU\Control Panel\Accessibility\StickyKeys` | `Flags` | `String` | `"26"` |
| `HKCU\Control Panel\Accessibility\Keyboard Response` | `Flags` | `String` | `"2"` |
| `HKCU\Control Panel\Accessibility\ToggleKeys` | `Flags` | `String` | `"34"` |

Đây là các bit-flag dạng chuỗi mà phần Accessibility của Windows đọc. Item không xóa tính năng
Accessibility khỏi Windows; nó đổi tổ hợp flag để vô hiệu hóa đường kích hoạt bằng phím tắt. Revert
khôi phục từng chuỗi và type cũ.

### 6.2 Security and Privacy

Source: [`Domain/Optimizations/Categories/SecurityAndPrivacy.cs`](Domain/Optimizations/Categories/SecurityAndPrivacy.cs)

#### DisableAdvertisingAndSuggestions

- ID: `B1C2D3E4-F5A6-4B7C-8D9E-0F1A2B3C4D5E`
- Risk: `Safe`
- Mục đích: tắt advertising ID, trải nghiệm cá nhân hóa dựa trên diagnostic data, consumer
  features, gợi ý hệ thống, thu thập dữ liệu nhập và một phần onboarding/SCOOBE.

| Hive/path | Value | Giá trị mới |
|---|---|---:|
| `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo` | `Enabled` | `0` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo` | `DisabledByGroupPolicy` | `1` |
| `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy` | `TailoredExperiencesWithDiagnosticDataEnabled` | `0` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent` | `DisableTailoredExperiencesWithDiagnosticData` | `1` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent` | `DisableWindowsConsumerFeatures` | `1` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent` | `DisableSoftLanding` | `1` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent` | `DisableThirdPartySuggestions` | `1` |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement` | `ScoobeSystemSettingEnabled` | `0` |
| `HKCU\SOFTWARE\Microsoft\InputPersonalization` | `RestrictImplicitInkCollection` | `1` |
| `HKCU\SOFTWARE\Microsoft\InputPersonalization` | `RestrictImplicitTextCollection` | `1` |
| `HKCU\SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore` | `HarvestContacts` | `0` |
| `HKCU\Control Panel\International\User Profile` | `HttpAcceptLanguageOptOut` | `1` |

Tất cả literal là `int`, nên `RegistryItem` tự suy ra `DWORD`. `0` thường là off; với tên policy
bắt đầu bằng `Disable` hoặc `Restrict`, `1` nghĩa là bật chính sách chặn. Mỗi dòng là một step độc
lập, vì vậy failure ở một khóa không xóa thông tin thành công/thất bại của các khóa còn lại.

#### DisableNewsAndInterests

- ID: `26F3AE39-4EAA-45D4-9375-B244159D5EB3`
- Risk: `Safe`
- Mục đích: tắt News and Interests trên Windows 10 và policy feed/widgets tương ứng trên các bản
  Windows có hỗ trợ.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKLM\SOFTWARE\Policies\Microsoft\Dsh` | `AllowNewsAndInterests` | `DWORD` | `0` |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Feeds` | `ShellFeedsTaskbarViewMode` | `DWORD` | `0` |

Một key là policy toàn máy (`HKLM`), key còn lại là cấu hình của user hiện tại (`HKCU`). Hiệu lực
thực tế phụ thuộc phiên bản Windows còn đọc các key này.

### 6.3 Bloatware and Services

Source: [`Domain/Optimizations/Categories/BloatwareAndServices.cs`](Domain/Optimizations/Categories/BloatwareAndServices.cs)

#### DisablePreinstalledApps

- ID: `E3C970B0-129D-4354-BAE4-2732E5BACCF7`
- Risk: `Safe`
- Mục đích: ngăn Content Delivery Manager tiếp tục tự phân phối hoặc cài ứng dụng được quảng bá.
- Không gỡ ứng dụng đã cài; chỉ đổi hành vi cài/phân phối về sau.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager` | `PreInstalledAppsEnabled` | `DWORD` | `0` |
| cùng path | `PreInstalledAppsEverEnabled` | `DWORD` | `0` |
| cùng path | `OemPreInstalledAppsEnabled` | `DWORD` | `0` |
| cùng path | `SilentInstalledAppsEnabled` | `DWORD` | `0` |

Đây là cấu hình per-user. Tên category có “Services”, nhưng item hiện tại không dừng hay disable
Windows service nào.

### 6.4 User Experience

Source: [`Domain/Optimizations/Categories/UserExperience.cs`](Domain/Optimizations/Categories/UserExperience.cs)

#### SpeedUpExplorerAndMenus

- ID: `9E2C4F7D-5A6B-4E9D-8C71-6E2F5A9C3314`
- Risk: `Safe`
- Mục đích: bỏ delay startup của Explorer và giảm menu hover delay về 0.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize` | `StartupDelayInMSec` | `DWORD` | `0` |
| `HKCU\Control Panel\Desktop` | `MenuShowDelay` | `String` | `"0"` |

Hai số 0 không cùng type: `StartupDelayInMSec` là `int` nên thành `DWORD`; `MenuShowDelay` được truyền
chuỗi nên giữ dạng `REG_SZ` mà Windows Desktop settings mong đợi.

#### DisableVisualEffects

- ID: `A3D1E8F2-7B4C-4A5D-9E62-8F3A1B7C4D59`
- Risk: `Safe`
- Mục đích: tắt một số hiệu ứng giao diện để giảm công việc vẽ của shell/DWM.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` | `TaskbarAnimations` | `DWORD` | `0` |
| cùng path | `ListviewShadow` | `DWORD` | `0` |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` | `EnableTransparency` | `DWORD` | `0` |
| `HKCU\Software\Microsoft\Windows\DWM` | `EnableAeroPeek` | `DWORD` | `0` |

Item chỉ tắt bốn hiệu ứng được liệt kê, không chuyển toàn bộ Windows sang preset “best
performance”. Mức cải thiện phụ thuộc GPU, CPU, driver và phiên bản Windows.

#### DisableStartMenuWebSearch

- ID: `950cd979-7483-400b-a207-abd4741a1270`
- Risk: `Safe`
- Mục đích: ngăn Start Search đưa suggestion/web result vào ô tìm kiếm.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKCU\Software\Policies\Microsoft\Windows\Explorer` | `DisableSearchBoxSuggestions` | `DWORD` | `1` |

Đây là policy per-user. Nó không xóa Windows Search index và không tắt service tìm kiếm local.

#### DisableStartRecommended

- ID: `31A6274C-AD37-4FFA-A32E-727FB71AD085`
- Risk: `Safe`
- Condition: `Windows11Condition`
- Mục đích: ẩn phần Recommended trong Start menu để không hiện recent files/apps.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer` | `HideRecommendedSection` | `DWORD` | `1` |

Windows 10 bị đánh dấu Unsupported bởi condition. Người dùng vẫn có thể ẩn cảnh báo và chạy, nhưng
Windows 10 không có UI tương ứng để key này tạo ra hiệu ứng hữu ích.

#### DisableSettings365Ads

- ID: `B36652B1-D094-4D39-BEB8-7961E6094A2E`
- Risk: `Safe`
- Mục đích: chặn nội dung quảng bá Microsoft 365/Copilot dựa trên account state trong Settings
  Home.

| Hive/path | Value | Type | Giá trị mới |
|---|---|---|---:|
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent` | `DisableConsumerAccountStateContent` | `DWORD` | `1` |

Đây là policy toàn máy, nên cần quyền admin. Nó nhắm vào nội dung quảng bá trong Settings, không gỡ
Microsoft 365 hay Copilot khỏi máy.

## 7. Debug optimizations

[`Domain/Optimizations/Categories/Debug.cs`](Domain/Optimizations/Categories/Debug.cs) nằm trong
`#if DEBUG`, nên không được compile vào Release. Các item này mô phỏng:

- một step lỗi rồi retry thành công;
- retry tiếp tục thất bại;
- lỗi không có retry;
- provider trả lỗi nhưng không ghi step;
- throw sau khi đã ghi một change;
- success bình thường;
- mọi thứ đã đúng nên chỉ có `Skip`.

Comment trong code xác nhận chúng không chạm máy. “Revert” debug là lệnh no-op `cmd /c exit 0`, chỉ
dùng để kiểm tra dialog, classification và persistence flow.

## 8. Các lớp an toàn và giới hạn cần hiểu đúng

### Những gì code bảo đảm

- Mỗi thao tác Registry đọc trạng thái cũ trước khi ghi.
- Nếu máy đã đúng, provider ghi `Skip` và không gọi mutation.
- Mỗi thay đổi có compensation riêng; revert chạy LIFO.
- Revert Registry có read-back verification.
- File revert được ghi atomic và có per-file lock.
- Partial success vẫn giữ dữ liệu để hoàn tác phần đã thay đổi.
- Retry thành công append backup mới, không ghi đè backup cũ.
- History chỉ là báo cáo; lỗi ghi history không đổi kết quả thao tác máy.
- Access denied được báo failure, không được coi là success.

### Những gì code không thể bảo đảm tuyệt đối

- Microsoft có thể đổi hoặc bỏ hỗ trợ một Registry policy ở bản Windows mới.
- Một số hiệu ứng cần reload Explorer, sign-out hoặc restart.
- Phần mềm khác, Group Policy, domain management hoặc Windows Update có thể ghi đè lại value.
- Revert chỉ hoàn tác các step đã lưu. Nếu người dùng/app khác thay đổi cùng value sau Apply, revert
  vẫn đưa value về snapshot trước Apply, không merge ý định của bên thứ ba.
- Restore Point có thể bị Windows từ chối, bị giới hạn tần suất hoặc System Protection đang tắt.
- Quyền admin không vượt qua mọi ACL/Windows protection.

## 9. Cách lần theo một optimization khi đọc code

Ví dụ với `DisableStartMenuWebSearch`:

1. Tìm nested class trong `Domain/Optimizations/Categories/UserExperience.cs`.
2. Đọc `[Optimization]` để biết GUID, risk, tags và condition.
3. Đọc `ApplyAsync` để thấy provider được gọi và target cụ thể.
4. Sang `RegistryService.Write` để xem read-before-write, skip, backup và error handling.
5. Sang `OperationRunner.RunAsync` để xem kết quả được phân loại và persist.
6. Sang `RegistryRevertStep.ExecuteAsync` để xem đường hoàn tác và verify.
7. Sang `OptimizationCategoryViewModel` để xem UI phản ứng với success/partial/failure/retry.
8. Mở file runtime trong `%LocalAppData%\fasterLegit\History\{id}.json` để xem lần chạy thực tế
   đã đổi value nào, từ gì sang gì.

Mẫu tư duy này áp dụng cho provider khác nếu project bổ sung lại service, scheduled task, shell,
power plan, hibernation hoặc USB: item khai báo ý định, provider sở hữu thao tác Windows, `ChangeSet`
ghi bằng chứng, `OperationRunner` sở hữu lifecycle, và `RevertManager` sở hữu hoàn tác.

## 10. Tóm tắt ngắn nhất

fasterLegit thay đổi máy được vì executable yêu cầu quyền Administrator và gọi API Windows thật.
Trong phần Optimize hiện tại, API đó là `Microsoft.Win32.RegistryKey`. Mỗi lần bật một card, app
đọc value cũ, chỉ ghi khi cần, lưu cách hoàn tác, ghi lịch sử, rồi để Windows/Explorer/Settings đọc
policy mới. Khi tắt card, app nạp backup theo GUID, chạy các bước ngược thứ tự và đọc lại Registry
để xác nhận trạng thái đã được khôi phục.
