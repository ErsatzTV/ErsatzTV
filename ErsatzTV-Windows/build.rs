use windres::Build;

fn main() {
    Build::new().compile("ersatztv_windows.rc").unwrap();
}
