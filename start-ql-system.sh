#!/bin/bash
# ==========================================================
#  QL System - مشغل التجربة المباشرة على Kali Linux
# ==========================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "==============================================================="
echo "   🚀 جاري تشغيل برنامج QL System للتجربة المباشرة...       "
echo "==============================================================="

# التأكد من تشغيل السيرفر المحلي
python3 "$SCRIPT_DIR/tools/preview/server.py" &
SERVER_PID=$!

sleep 1

echo "✓ تم تشغيل محرك البرنامج وقاعدة البيانات بنجاح!"
echo "✓ جاري فتح الواجهة في المتصفح..."
echo ""
echo "👉 الرابط المباشر: http://localhost:5050"
echo "==============================================================="
echo "اضغط Ctrl+C لإيقاف البرنامج."

# فتح المتصفح تلقائياً
if which xdg-open > /dev/null; then
    xdg-open "http://localhost:5050" 2>/dev/null &
elif which firefox > /dev/null; then
    firefox "http://localhost:5050" 2>/dev/null &
fi

wait $SERVER_PID
