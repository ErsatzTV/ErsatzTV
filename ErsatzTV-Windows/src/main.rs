#![windows_subsystem = "windows"]

use special_folder::SpecialFolder;
use std::fs;
use std::os::windows::process::CommandExt;
use std::process::Child;
use std::process::Command;
use std::process::Stdio;
use windows::Win32::System::Console;
use {std::sync::mpsc, tray_item::TrayItem};

const CREATE_NO_WINDOW: u32 = 0x08000000;

enum Message {
    Exit,
}

fn main() {
    let mut tray = TrayItem::new("ErsatzTV", "ersatztv-icon").unwrap();

    let (tx, rx) = mpsc::channel();

    tray.add_menu_item("Launch Web UI", || {
        let _ = Command::new("cmd")
            .creation_flags(CREATE_NO_WINDOW)
            .arg("/C")
            .arg("start")
            .arg("http://localhost:8409")
            .stdin(Stdio::null())
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .spawn();
    })
        .unwrap();

    tray.add_menu_item("Show Logs", || {
        let path = SpecialFolder::LocalApplicationData
            .get()
            .unwrap()
            .join("ersatztv")
            .join("logs");
        match path.to_str() {
            None => {}
            Some(folder) => {
                fs::create_dir_all(folder).unwrap();
                let _ = Command::new("explorer.exe")
                    .arg(folder)
                    .stdin(Stdio::null())
                    .stdout(Stdio::null())
                    .stderr(Stdio::null())
                    .spawn();
            }
        }
    })
        .unwrap();

    tray.inner_mut().add_separator().unwrap();

    tray.add_menu_item("Exit", move || {
        tx.send(Message::Exit).unwrap();
    })
        .unwrap();

    let path = process_path::get_executable_path();
    let mut child: Option<Child> = None;
    match path {
        None => {}
        Some(path) => {
            let etv = path.parent().unwrap().join("ErsatzTV.exe");
            if etv.exists() {
                match etv.to_str() {
                    None => {}
                    Some(etv) => {
                        child = Some(
                            Command::new(etv)
                                .creation_flags(CREATE_NO_WINDOW)
                                .stdin(Stdio::null())
                                .stdout(Stdio::null())
                                .stderr(Stdio::null())
                                .spawn()
                                .unwrap(),
                        );
                    }
                }
            }
        }
    }

    loop {
        match rx.recv() {
            Ok(Message::Exit) => {
                match child {
                    None => {}
                    Some(mut child) => {
                        unsafe {
                            if Console::AttachConsole(child.id()) == true
                            {
                                Console::GenerateConsoleCtrlEvent(Console::CTRL_C_EVENT, 0);
                            }
                        }
                        child.wait().unwrap();
                    }
                }
                break;
            }
            _ => {}
        }
    }
}
