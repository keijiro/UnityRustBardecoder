use std::ffi::CString;
use std::os::raw::c_char;

static mut DECODE_BUFFER: Vec<u8> = vec![];

#[no_mangle]
pub unsafe extern fn bardecoder_decode
  (raw_rgba: *const u8, width: i32, height: i32) -> *const c_char {
    let storage = std::slice::from_raw_parts(raw_rgba, (width * height * 4) as usize);
    let buffer = image::ImageBuffer::from_raw(width as u32, height as u32, storage.to_vec()).unwrap();
    let image = image::DynamicImage::ImageRgba8(buffer);

    let decoder = bardecoder::default_decoder();
    let results = decoder.decode(&image);

    for result in results {
        if result.is_ok() {
            let string = CString::new(result.unwrap()).unwrap();
            let bytes = string.as_bytes_with_nul();
            DECODE_BUFFER.resize(bytes.len(), 0);
            DECODE_BUFFER.copy_from_slice(bytes);
            return DECODE_BUFFER.as_ptr() as *const c_char;
        }
    }

    std::ptr::null_mut()
}
