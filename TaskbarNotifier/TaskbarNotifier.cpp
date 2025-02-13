#include "pch.h"
#define MSG_MAX 0x501
#define MSG_UNMAX 0x502


APPBARDATA msg_data = { sizeof(APPBARDATA) };

WINDOWPLACEMENT placement = { sizeof(WINDOWPLACEMENT) };

POINT cursor;

HWND max_window;

DWORD pid;

BOOL cloaked_val = TRUE;

bool try_show_bar = true;

inline bool is_cursor_over_taskbar()
{
    // Do not change the status of the taskbar when the mouse is over it.

    // 1. Users may switch between multiple tasks at this time, switching the status of the Taskbar will affect the user experience.
    // 2. In win10, switching the status of the Taskbar to Auto-Hide Mode while the mouse is over it will causes a bug.
    //      which somehow causes the taskbar to blocks the content below it.
    //      (The taskbar cannot be hidden at this situation unless the user clicks on the taskbar and then clicks on another window.)

    // For the above reasons, at the beginning of this infinite loop, we must first determine whether the mouse is above the Taskbar.

    // 当鼠标在任务栏上方时，不应该改变任务栏的状态

    // 1. 此时用户可能在多个窗体之间切换不定，这时候改变任务栏状态会影响用户体验
    // 2. 在win10中，当鼠标在任务栏上方时切换任务栏到自动隐藏模式会导致bug，这个bug会使得任务栏遮挡住它下方的内容，而不能正确的隐藏
    //      要解决这个问题，需要用户点击一次任务栏，再点击一次其他窗口，才能使得任务栏正常隐藏

    // 综上，在循环的开始，需要检测鼠标是否在任务栏上方
    GetCursorPos(&cursor);
    switch (msg_data.uEdge)
    {
    case ABE_BOTTOM:
        if (cursor.y >= msg_data.rc.top)
            return true;
        return false;
    case ABE_LEFT:
        if (cursor.x <= msg_data.rc.right)
            return true;
        return false;
    case ABE_TOP:
        if (cursor.y <= msg_data.rc.bottom)
            return true;
        return false;
    default:
        if (cursor.x >= msg_data.rc.left)
            return true;
        return false;
    }
}

BOOL CALLBACK enum_windows_proc(const HWND hwnd, LPARAM)
{
    if (IsWindowVisible(hwnd) == FALSE) // Skip hidden windows, IsWindowVisible is much faster than GetWindowPlacement (approximately 4 times).
        return TRUE;                    // 跳过隐藏的窗体，IsWindowVisible 比 GetWindowPlacement 大约快4倍
    GetWindowPlacement(hwnd, &placement);
    if (placement.showCmd != SW_MAXIMIZE) // Skip not maximized windows.
        return TRUE;                      // 跳过不是最大化的窗体
    // The IsVisible property of some applications is true but the user cannot see these applications.
    // These applications may be:

    // 1. In another virtual desktop.
    // 2. is UWP application (Application Frame Host is always in maximized and visible state whenever there is a UWP application (even if it's suspend) is maximized).
    // DwmGetWindowAttribute therefore must be used to further determine whether the window is truly visible.

    // 有些窗体在Visible属性为true时，用户仍然看不见它们
    // 这可能是因为：

    // 1. 它在另一个虚拟桌面
    // 2. 它是一个UWP应用，UWP应用的宿主Application Frame Host，会保持可见属性和最大化属性，只要有任何一个UWP应用是最大化的
    // 因而必须使用DwmGetWindowAttribute对窗体是否可见作进一步判断
    DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, &cloaked_val, sizeof(cloaked_val));
    if (cloaked_val)
        return TRUE;
    max_window = hwnd;
    return FALSE;
}


int wmain(const int argc, wchar_t* argv[])
{
    if (argc < 2)
        return 0;
    pid = _wtoi(argv[1]);
    SHAppBarMessage(ABM_GETTASKBARPOS, &msg_data);
    while (true) {
        while (is_cursor_over_taskbar())
            Sleep(250);
        EnumWindows(enum_windows_proc, NULL);
        if (max_window == nullptr) {
            if (try_show_bar == false) {
                Sleep(375);
                continue;
            }
            try_show_bar = false;
            if (PostThreadMessage(pid, MSG_UNMAX, NULL, NULL) == FALSE)
                return 0;
            Sleep(500);
            continue;
        }
        if (PostThreadMessage(pid, MSG_MAX, NULL, NULL) == FALSE)
            return 0;
        do {
            Sleep(500);
            if (IsWindowVisible(max_window) == FALSE)
                break;
            DwmGetWindowAttribute(max_window, DWMWA_CLOAKED, &cloaked_val, sizeof(cloaked_val));
            if (cloaked_val)
                break;
            GetWindowPlacement(max_window, &placement);
        } while (placement.showCmd == SW_MAXIMIZE);
        try_show_bar = true;
        max_window = nullptr;
        SHAppBarMessage(ABM_GETTASKBARPOS, &msg_data);
    }
}
