// Nội dung các trang thông tin tĩnh (footer). Mỗi trang = tiêu đề + các mục.
export interface InfoSection { heading?: string; paragraphs?: string[]; list?: string[] }
export interface InfoPage { icon: string; title: string; intro?: string; sections: InfoSection[]; updated?: string }

export const infoPages: Record<string, InfoPage> = {
  help: {
    icon: 'pi-question-circle',
    title: 'Trung tâm trợ giúp',
    intro: 'Giải đáp nhanh các thắc mắc thường gặp khi mua điện thoại tại PhoneStore.',
    sections: [
      { heading: 'Câu hỏi thường gặp', list: [
        'Làm sao để đặt máy? — Chọn màu & dung lượng, thêm vào giỏ, chọn địa chỉ & phương thức thanh toán rồi bấm "Đặt hàng".',
        'Tôi có cần tài khoản để mua không? — Bạn có thể thêm vào giỏ khi chưa đăng nhập; khi thanh toán mới cần đăng nhập.',
        'Kiểm tra đơn hàng ở đâu? — Vào "Đơn hàng của tôi" để xem trạng thái và lịch sử từng đơn.',
        'Máy có được kích hoạt bảo hành không? — Có. Khi giao máy, mỗi máy được gán IMEI và tạo phiếu bảo hành, tra cứu ở mục "Tra cứu bảo hành".'
      ] },
      { heading: 'Vẫn cần hỗ trợ?', paragraphs: [
        'Liên hệ tổng đài 1900 6035 (8:00–22:00 hằng ngày) hoặc email hotro@phonestore.vn — chúng tôi phản hồi trong vòng 24 giờ.'
      ] }
    ]
  },
  guide: {
    icon: 'pi-shopping-bag',
    title: 'Hướng dẫn mua hàng',
    intro: 'Chỉ 4 bước đơn giản để hoàn tất đơn mua điện thoại.',
    sections: [
      { list: [
        'Bước 1 — Tìm & chọn máy: dùng thanh tìm kiếm hoặc lọc theo thương hiệu/dung lượng, chọn màu & dung lượng phù hợp.',
        'Bước 2 — Thêm vào giỏ: chọn số lượng rồi bấm "Thêm vào giỏ", hoặc "Mua ngay" để đến thẳng thanh toán.',
        'Bước 3 — Thanh toán: chọn địa chỉ nhận hàng, phương thức vận chuyển, nhập mã giảm giá (nếu có).',
        'Bước 4 — Chọn cách trả tiền: COD (trả khi nhận), chuyển khoản VNPAY, hoặc trả góp 6/9/12 tháng với máy hỗ trợ.'
      ] },
      { heading: 'Mẹo nhỏ', paragraphs: [
        'Lưu địa chỉ mặc định để lần sau đặt nhanh hơn, và theo dõi trạng thái đơn ngay trong mục "Đơn hàng của tôi".'
      ] }
    ]
  },
  returns: {
    icon: 'pi-replay',
    title: 'Trả hàng & hoàn tiền',
    intro: 'Chính sách đổi trả minh bạch, bảo vệ quyền lợi người mua.',
    sections: [
      { heading: 'Điều kiện đổi trả', list: [
        'Trong vòng 7 ngày kể từ khi nhận hàng.',
        'Máy còn nguyên seal/tem, chưa kích hoạt, đầy đủ hộp & phụ kiện đi kèm.',
        'Có hình ảnh/video nếu máy bị lỗi, hư hỏng hoặc giao sai.'
      ] },
      { heading: 'Cách hoàn tiền', paragraphs: [
        'Với đơn COD: hoàn qua chuyển khoản trong 3–5 ngày làm việc sau khi nhận hàng trả.',
        'Với đơn đã chuyển khoản: hoàn về đúng tài khoản đã thanh toán trong 3–7 ngày làm việc.'
      ] }
    ]
  },
  warranty: {
    icon: 'pi-verified',
    title: 'Chính sách bảo hành',
    intro: 'Cam kết máy chính hãng, bảo hành theo IMEI và nhà sản xuất.',
    sections: [
      { list: [
        'Điện thoại: bảo hành 12–24 tháng theo hãng, gắn theo IMEI của từng máy.',
        'Phụ kiện: đổi mới trong 30 ngày nếu lỗi do nhà sản xuất.',
        'Không áp dụng cho hư hỏng do người dùng, rơi vỡ, vào nước hoặc hết thời hạn bảo hành.'
      ] },
      { heading: 'Kích hoạt & tra cứu bảo hành', paragraphs: [
        'Mỗi máy khi giao được gán IMEI và tạo phiếu bảo hành tự động. Tra cứu tình trạng bảo hành theo IMEI hoặc mã đơn tại mục "Tra cứu bảo hành".'
      ] }
    ]
  },
  about: {
    icon: 'pi-building',
    title: 'Giới thiệu PhoneStore',
    intro: 'Hệ thống bán lẻ điện thoại chính hãng, đồng hành cùng người dùng Việt.',
    sections: [
      { paragraphs: [
        'PhoneStore mang đến điện thoại chính hãng từ Apple, Samsung, Xiaomi, OPPO... với giá minh bạch, đánh giá thật, bảo hành theo IMEI và giao hàng nhanh trên toàn quốc.'
      ] },
      { heading: 'Chúng tôi hướng tới', list: [
        'Trải nghiệm mua máy nhanh, gọn, dễ dùng.',
        'Máy chính hãng, giá minh bạch, hỗ trợ trả góp.',
        'Thanh toán an toàn và bảo hành đáng tin cậy.'
      ] }
    ]
  },
  terms: {
    icon: 'pi-file',
    title: 'Điều khoản sử dụng',
    intro: 'Vui lòng đọc kỹ trước khi sử dụng dịch vụ.',
    sections: [
      { heading: '1. Chấp nhận điều khoản', paragraphs: ['Khi truy cập và sử dụng PhoneStore, bạn đồng ý tuân thủ các điều khoản này và các chính sách liên quan.'] },
      { heading: '2. Tài khoản', paragraphs: ['Bạn chịu trách nhiệm bảo mật thông tin đăng nhập và mọi hoạt động phát sinh từ tài khoản của mình.'] },
      { heading: '3. Đặt hàng & thanh toán', paragraphs: ['Giá và khuyến mãi có thể thay đổi. Đơn hàng chỉ được xác nhận sau khi hệ thống ghi nhận thành công.'] },
      { heading: '4. Hành vi bị cấm', paragraphs: ['Không gian lận, phá hoại hệ thống, hoặc sử dụng dịch vụ vào mục đích trái pháp luật.'] }
    ]
  },
  privacy: {
    icon: 'pi-lock',
    title: 'Chính sách bảo mật',
    intro: 'Chúng tôi tôn trọng và bảo vệ dữ liệu cá nhân của bạn.',
    sections: [
      { heading: 'Thông tin thu thập', list: ['Thông tin tài khoản (email, họ tên, số điện thoại).', 'Địa chỉ giao hàng.', 'Lịch sử đơn hàng và đánh giá.'] },
      { heading: 'Cách sử dụng', paragraphs: ['Dữ liệu chỉ dùng để xử lý đơn hàng, chăm sóc khách hàng và cải thiện dịch vụ — không bán cho bên thứ ba.'] },
      { heading: 'Bảo mật', paragraphs: ['Mật khẩu được băm một chiều; thanh toán qua cổng bảo mật; không lưu thông tin thẻ trên hệ thống.'] }
    ]
  },
  contact: {
    icon: 'pi-envelope',
    title: 'Liên hệ hợp tác',
    intro: 'Bạn muốn hợp tác, bán hàng hoặc cần hỗ trợ? Liên hệ với chúng tôi.',
    sections: [
      { heading: 'Thông tin liên hệ', list: [
        'Email: hoptac@phonestore.vn',
        'Hotline: 1900 xxxx (8:00–22:00)',
        'Địa chỉ: Số 123, Đường ABC, Phường XYZ, TP. Demo (địa chỉ minh hoạ)',
        'Giờ làm việc: Thứ 2 – Chủ nhật, 8:00 – 22:00'
      ] }
    ]
  }
}
