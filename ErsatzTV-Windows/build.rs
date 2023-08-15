use windres::Build;

fn main() {
    static_vcruntime::metabuild();
    Build::new().compile("ersatztv_windows.rc").unwrap();
}
