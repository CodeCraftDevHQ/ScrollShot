using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ScrollShot
{
    public static class Loc
    {
        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>();

        public static string Language { get; private set; } = "en";
        public static bool IsRtl { get { return Language == "fa"; } }
        public static FlowDirection Flow { get { return IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; } }
        public static event Action Changed;

        public static void SetLanguage(string lang)
        {
            Language = lang == "fa" ? "fa" : "en";
            LocalizationSource.Instance.Refresh();
            var h = Changed;
            if (h != null) h();
        }

        public static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            return IsRtl ? v[1] : v[0];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        static void A(string key, string en, string fa)
        {
            S[key] = new[] { en, fa };
        }

        static Loc()
        {
            A("tray_region", "Capture region", "گرفتن از ناحیه");
            A("tray_window", "Capture window", "گرفتن از پنجره");
            A("tray_full", "Full screen", "تمام صفحه");
            A("tray_scroll", "Scrolling screenshot", "اسکرین‌شات اسکرولی");
            A("tray_delayed", "Full screen with delay", "تمام صفحه با تأخیر");
            A("tray_folder", "Open save folder", "باز کردن پوشه ذخیره");
            A("tray_settings", "Settings", "تنظیمات");
            A("tray_exit", "Exit", "خروج");

            A("msg_hotkey_title", "Shortcut unavailable", "میانبر در دسترس نیست");
            A("msg_hotkey_text", "These shortcuts are used by another app: {0}", "این میانبرها توسط برنامه دیگری استفاده می‌شوند: {0}");
            A("msg_error", "Error", "خطا");
            A("msg_save_error", "Save error", "خطا در ذخیره");
            A("msg_sep", ", ", "، ");

            A("toast_saved", "Screenshot saved", "اسکرین‌شات ذخیره شد");
            A("toast_copied", "Copied to clipboard", "در کلیپ‌بورد کپی شد");
            A("toast_taken", "Screenshot taken", "اسکرین‌شات گرفته شد");
            A("toast_files", "{0} files", "{0} فایل");
            A("toast_copied_suffix", "copied", "کپی شد");
            A("btn_open", "Open", "باز کردن");
            A("btn_folder", "Folder", "پوشه");
            A("btn_delete", "Delete", "حذف");
            A("btn_close", "Close", "بستن");

            A("bar_title", "Scrolling screenshot", "اسکرین‌شات اسکرولی");
            A("bar_auto", "Start auto", "شروع خودکار");
            A("bar_now", "Start now", "شروع همین الان");
            A("bar_manual", "Manual mode", "حالت دستی");
            A("bar_done", "Finish & save", "پایان و ذخیره");
            A("bar_cancel", "Cancel", "لغو");
            A("bar_top", "Scroll to top first", "قبل از شروع، صفحه را به بالا برگردان");
            A("bar_hint", "Esc = cancel   |   press the scroll shortcut again = finish", "Esc = لغو   |   فشردن دوباره میانبر اسکرولی = پایان");
            A("bar_ready", "Ready. Choose a mode.", "آماده است. حالت را انتخاب کنید.");
            A("bar_countdown", "Starting {0} mode in {1}s… position the area first.", "شروع {0} تا {1} ثانیه دیگر… ابتدا ناحیه را در جای دلخواه قرار دهید.");
            A("bar_to_top", "Scrolling back to top…", "در حال برگرداندن صفحه به بالا…");
            A("bar_building", "Building final image…", "در حال ساخت تصویر نهایی…");
            A("bar_stats", "Frames: {0}   |   Height: {1} px", "فریم‌ها: {0}   |   ارتفاع: {1} px");
            A("bar_adjust", "Adjusting scroll step… (attempt {0})", "تنظیم خودکار گام اسکرول… (تلاش {0})");
            A("bar_manual_hint", "Now scroll down slowly yourself, then press “Finish & save”.", "حالا خودتان صفحه را آرام به پایین اسکرول کنید؛ در پایان «پایان و ذخیره».");
            A("bar_manual_fail", "Scrolled up or too fast; scroll down slowly again.", "اسکرول به بالا یا خیلی سریع بود؛ دوباره آرام به پایین بروید.");
            A("end_height", "Reached the maximum height.", "به حداکثر ارتفاع رسید.");
            A("end_frames", "Reached the maximum number of frames.", "به حداکثر تعداد فریم رسید.");
            A("end_match", "Could not match the images. This area may not suit stitching; try manual mode.", "تطبیق تصاویر ناموفق بود. محتوای این ناحیه برای چسباندن مناسب نیست؛ حالت دستی را امتحان کنید.");

            A("settings_title", "ScrollShot Settings", "تنظیمات ScrollShot");
            A("nav_general", "General", "عمومی");
            A("nav_output", "Size & PDF", "حجم و PDF");
            A("nav_hotkeys", "Shortcuts & button", "میانبرها و دکمه");
            A("nav_scrolling", "Scrolling", "اسکرول");
            A("nav_appearance", "Appearance", "ظاهر");
            A("btn_save", "Save", "ذخیره");
            A("btn_cancel", "Cancel", "انصراف");
            A("btn_reset", "Reset to defaults", "بازنشانی به پیش‌فرض");
            A("btn_browse", "Browse…", "انتخاب…");
            A("color_invalid", "The color value is not valid. Example: #FF3B82F6", "رنگ واردشده معتبر نیست. نمونه: #FF3B82F6");

            A("sec_saving", "Saving", "ذخیره‌سازی");
            A("gen_folder", "Save folder", "پوشه ذخیره");
            A("gen_pattern", "File name pattern", "الگوی نام فایل");
            A("gen_pattern_tip", "Tokens: {date} {time} {type}", "متغیرها: {date} {time} {type}");
            A("gen_format", "Image format", "فرمت تصویر");
            A("gen_quality", "JPG quality", "کیفیت JPG");
            A("gen_save_file", "Save to file", "ذخیره در فایل");
            A("gen_clipboard", "Copy to clipboard", "کپی در کلیپ‌بورد");
            A("sec_after", "After capture", "بعد از گرفتن");
            A("gen_toast", "Show preview after capture", "نمایش پیش‌نمایش پس از گرفتن");
            A("gen_toast_sec", "Preview duration (seconds)", "مدت نمایش پیش‌نمایش (ثانیه)");
            A("gen_sound", "Play sound", "پخش صدا");
            A("gen_startup", "Start with Windows", "اجرا همراه ویندوز");
            A("sec_capture", "Capture", "گرفتن تصویر");
            A("gen_full_scope", "Full screen from", "تمام صفحه از");
            A("scope_cursor", "The monitor under the mouse", "مانیتوری که ماوس روی آن است");
            A("scope_all", "All monitors", "همه مانیتورها");
            A("gen_delay", "Capture delay (seconds)", "تأخیر گرفتن (ثانیه)");

            A("sec_size", "Reduce file size", "کاهش حجم");
            A("out_scale", "Image scale (%)", "مقیاس تصویر (٪)");
            A("out_maxw", "Maximum width (px, 0 = off)", "حداکثر عرض (px، 0 = خاموش)");
            A("out_target", "Target size (KB, 0 = off)", "حجم هدف (KB، 0 = خاموش)");
            A("out_target_tip", "Per file for JPG, whole file for PDF. Has no effect on PNG; use scale instead.", "برای JPG هر فایل، و برای PDF کل فایل. در PNG اثری ندارد؛ از مقیاس استفاده کنید.");
            A("sec_split", "Split long images", "تقسیم تصویر بلند");
            A("out_split", "Split long images into several files", "تقسیم خودکار تصاویر بلند به چند فایل");
            A("out_split_h", "Maximum height per part (px)", "حداکثر ارتفاع هر بخش (px)");
            A("out_smart", "Smart cut (avoid cutting through text or images)", "برش هوشمند (از وسط متن و تصویر نبُرد)");
            A("sec_pdf", "PDF", "PDF");
            A("out_alsopdf", "Also save a PDF next to the image", "علاوه بر تصویر، PDF هم ذخیره شود");
            A("out_pdf_layout", "Page layout", "چیدمان صفحه");
            A("pdf_multi", "Multiple standard pages", "چند صفحه استاندارد");
            A("pdf_single", "One long page", "یک صفحه بلند");
            A("out_pdf_size", "Page size", "اندازه صفحه");
            A("out_pdf_margin", "Page margin (mm)", "حاشیه صفحه (mm)");
            A("out_pdf_q", "Image quality in PDF", "کیفیت تصویر در PDF");
            A("out_pdf_note", "If a single long page exceeds the PDF limit, it is split into several pages automatically.", "اگر «یک صفحه بلند» از حد مجاز PDF بزرگ‌تر شود، خودکار به چند صفحه تبدیل می‌شود.");

            A("hk_hint", "Click a box and press the key combination. Backspace clears it.", "روی کادر کلیک کنید و ترکیب کلید را بزنید. Backspace برای پاک کردن.");
            A("sec_hotkeys", "Global shortcuts", "میانبرهای سراسری");
            A("hk_region", "Capture region", "گرفتن از ناحیه");
            A("hk_window", "Capture window", "گرفتن از پنجره");
            A("hk_full", "Full screen", "تمام صفحه");
            A("hk_scroll", "Scrolling screenshot", "اسکرین‌شات اسکرولی");
            A("hk_tray_dbl", "Double-click on tray icon", "دابل‌کلیک روی آیکون tray");
            A("act_region", "Capture region", "گرفتن از ناحیه");
            A("act_window", "Capture window", "گرفتن از پنجره");
            A("act_full", "Full screen", "تمام صفحه");
            A("act_scroll", "Scrolling screenshot", "اسکرولی");
            A("act_settings", "Settings", "تنظیمات");
            A("sec_floating", "Floating button", "دکمه شناور");
            A("fl_show", "Show floating button", "نمایش دکمه شناور");
            A("fl_size", "Button size", "اندازه دکمه");
            A("fl_opacity", "Button opacity", "شفافیت دکمه");

            A("sec_sc_start", "Starting", "شروع");
            A("nav_profiles", "Profiles", "پروفایل‌ها");
            A("sec_profiles", "Saved profiles", "پروفایل‌های ذخیره‌شده");
            A("pf_hint", "A profile stores the saving, size, PDF and scrolling options. Pick one and apply it, or save the current options under a new name.", "پروفایل شامل گزینه‌های ذخیره، حجم، PDF و اسکرول است. یکی را انتخاب و اعمال کنید، یا گزینه‌های فعلی را با نام جدید ذخیره کنید.");
            A("pf_apply", "Apply", "اعمال");
            A("pf_delete", "Delete", "حذف");
            A("pf_save", "Save current options", "ذخیره گزینه‌های فعلی");
            A("pf_active", "Active profile: {0}", "پروفایل فعال: {0}");
            A("tray_profiles", "Profiles", "پروفایل‌ها");
            A("hk_profile", "Switch to next profile", "رفتن به پروفایل بعدی");
            A("msg_profile_applied", "Profile applied: {0}", "پروفایل اعمال شد: {0}");
            A("sc_direction", "Scroll direction", "جهت اسکرول");
            A("dir_vertical", "Vertical", "عمودی");
            A("dir_horizontal", "Horizontal", "افقی");
            A("sc_mode", "Default mode", "حالت پیش‌فرض");
            A("mode_auto", "Automatic", "خودکار");
            A("mode_manual", "Manual", "دستی");
            A("sc_countdown", "Countdown before start (seconds, 0 = wait for click)", "شمارش معکوس قبل از شروع (ثانیه، 0 = منتظر کلیک)");
            A("sc_top", "Scroll back to the top before starting", "قبل از شروع، صفحه را به بالا برگردان");
            A("sc_mouse", "Scroll without moving the mouse (falls back automatically)", "اسکرول بدون جابه‌جا کردن ماوس (در صورت نپذیرفتن، خودکار روش قبلی)");
            A("sec_sc_tuning", "Tuning", "تنظیم دقیق");
            A("sc_notches", "Wheel notches per step (auto-adjusted)", "نوچ اسکرول در هر گام (خودکار تنظیم می‌شود)");
            A("sc_delay", "Delay after each scroll (ms)", "تأخیر بعد از هر اسکرول (ms)");
            A("sc_stable", "Max wait for the page to settle (ms)", "حداکثر صبر برای ثابت شدن (ms)");
            A("sc_maxh", "Maximum image height (px)", "حداکثر ارتفاع تصویر (px)");
            A("sc_maxframes", "Maximum number of frames", "حداکثر تعداد فریم");
            A("sc_ignr", "Ignore right edge (px)", "نادیده گرفتن لبه راست (px)");
            A("sc_ignr_tip", "Useful to skip the scrollbar", "برای رد کردن اسکرول‌بار");
            A("sc_ignl", "Ignore left edge (px)", "نادیده گرفتن لبه چپ (px)");
            A("sc_tol", "Match tolerance (1 to 30)", "تلورانس تطبیق (۱ تا ۳۰)");

            A("sec_language", "Language & theme", "زبان و تم");
            A("ap_language", "Language", "زبان");
            A("ap_theme", "Theme", "تم");
            A("theme_dark", "Dark", "تاریک");
            A("theme_light", "Light", "روشن");
            A("theme_system", "Follow system", "مطابق سیستم");
            A("ap_accent", "Accent color (hex)", "رنگ تأکیدی (hex)");
            A("sec_selection", "Selection overlay", "پوشش انتخاب ناحیه");
            A("ap_dim", "Background dimming", "تیرگی پس‌زمینه");
            A("ap_selcolor", "Selection border color (hex)", "رنگ کادر انتخاب (hex)");
            A("ap_thick", "Border thickness", "ضخامت کادر");
            A("ap_framecolor", "Scroll frame color (hex)", "رنگ کادر اسکرول (hex)");
            A("ap_size", "Show selection size", "نمایش اندازه ناحیه");
            A("ap_cross", "Show crosshair guides", "نمایش خطوط راهنما (crosshair)");
            A("ap_hl", "Highlight the window under the mouse (hold Ctrl for inner panes)", "هایلایت پنجره زیر ماوس (با Ctrl: بخش داخلی پنجره)");
        }
    }

    public sealed class LocalizationSource : INotifyPropertyChanged
    {
        public static LocalizationSource Instance { get; } = new LocalizationSource();

        public event PropertyChangedEventHandler PropertyChanged;

        public string this[string key]
        {
            get { return Loc.T(key); }
        }

        public FlowDirection Flow
        {
            get { return Loc.Flow; }
        }

        public void Refresh()
        {
            var h = PropertyChanged;
            if (h == null) return;
            h(this, new PropertyChangedEventArgs("Item[]"));
            h(this, new PropertyChangedEventArgs("Flow"));
        }
    }

    public sealed class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension()
        {
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var b = new Binding("[" + Key + "]")
            {
                Source = LocalizationSource.Instance,
                Mode = BindingMode.OneWay
            };
            return b.ProvideValue(serviceProvider);
        }
    }
}
