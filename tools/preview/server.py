#!/usr/bin/env python3
import http.server
import socketserver
import json
import sqlite3
import os
import hmac
import hashlib
from datetime import datetime, timedelta
import urllib.parse

PORT = 5050
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(os.path.dirname(BASE_DIR))
DB_PATH = os.path.join(PROJECT_DIR, "test_data", "quincaillerie.db")
SECRET_KEY = "QL_SECRET_DEVELOPER_KEY_ALGERIA_QUINCAILLERIE_2026_X99"

# Generate local machine ID
def get_machine_id():
    try:
        with open("/etc/machine-id") as f:
            mid = f.read().strip()
    except:
        mid = "KALI-LINUX-DEV-MACHINE"
    h = hashlib.sha256((mid + "QL_QUINCAILLERIE_SYSTEM_DZ_2026_SECURE_SALT").encode()).hexdigest().upper()
    return f"QL-ALG-{h[:4]}-{h[4:8]}-{h[8:12]}"

MACHINE_ID = get_machine_id()
LICENSE_FILE = os.path.join(PROJECT_DIR, "test_data", "license", "license.dat")

def get_db():
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn

def check_license():
    is_activated = False
    status = "TrialActive"
    remaining_days = 7

    if os.path.exists(LICENSE_FILE):
        try:
            with open(LICENSE_FILE) as f:
                lines = [line.strip() for line in f.readlines()]
            if len(lines) >= 2 and lines[1] != "NONE":
                expected = generate_activation_key(MACHINE_ID)
                if lines[1].upper() == expected.upper():
                    is_activated = True
                    status = "Activated"
                    remaining_days = 9999
        except:
            pass

    return {
        "machineId": MACHINE_ID,
        "status": status,
        "isActivated": is_activated,
        "remainingDays": remaining_days,
        "whatsapp": "0550 12 34 56"
    }

def generate_activation_key(mid):
    sig = hmac.new(SECRET_KEY.encode(), mid.strip().upper().encode(), hashlib.sha256).hexdigest().upper()
    return f"ACT-{sig[:4]}-{sig[4:8]}-{sig[8:12]}-{sig[12:16]}"

class QLRequestHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(200)
        self.end_headers()

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        params = urllib.parse.parse_qs(parsed.query)

        if parsed.path == "/" or parsed.path == "/index.html":
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.end_headers()
            with open(os.path.join(BASE_DIR, "index.html"), "rb") as f:
                self.wfile.write(f.read())
            return

        if parsed.path.startswith("/static/") or parsed.path.startswith("/webfonts/"):
            file_path = os.path.join(BASE_DIR, parsed.path.lstrip("/"))
            if os.path.exists(file_path) and os.path.isfile(file_path):
                self.send_response(200)
                if file_path.endswith(".css"):
                    self.send_header('Content-Type', 'text/css')
                elif file_path.endswith(".woff2"):
                    self.send_header('Content-Type', 'font/woff2')
                self.end_headers()
                with open(file_path, "rb") as f:
                    self.wfile.write(f.read())
                return

        if parsed.path == "/api/status":
            self.send_json(check_license())
            return

        if parsed.path == "/api/products":
            q = params.get('q', [''])[0].strip()
            low = params.get('low', ['false'])[0].lower() == 'true'
            conn = get_db()
            cur = conn.cursor()

            if low:
                cur.execute("SELECT p.*, c.Name as CategoryName FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id WHERE p.StockQuantity <= p.MinStockAlert AND p.IsActive = 1 ORDER BY p.StockQuantity ASC")
            elif q:
                like_q = f"%{q}%"
                cur.execute("""
                    SELECT p.*, c.Name as CategoryName FROM Products p 
                    LEFT JOIN Categories c ON p.CategoryId = c.Id 
                    WHERE p.IsActive = 1 AND (p.Barcode = ? OR p.Reference LIKE ? OR p.Name LIKE ? OR p.Dimensions LIKE ?)
                    ORDER BY CASE WHEN p.Barcode = ? THEN 1 WHEN p.Reference LIKE ? THEN 2 ELSE 3 END, p.Name ASC LIMIT 30
                """, (q, like_q, like_q, like_q, q, f"{q}%"))
            else:
                cur.execute("SELECT p.*, c.Name as CategoryName FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id WHERE p.IsActive = 1 ORDER BY p.Name ASC")

            rows = [dict(r) for r in cur.fetchall()]
            conn.close()
            self.send_json(rows)
            return

        if parsed.path == "/api/customers":
            conn = get_db()
            cur = conn.cursor()
            cur.execute("SELECT * FROM Customers ORDER BY CurrentDebt DESC, FullName ASC")
            rows = [dict(r) for r in cur.fetchall()]
            conn.close()
            self.send_json(rows)
            return

        if parsed.path == "/api/payments":
            cust_id = params.get('customerId', [None])[0]
            if not cust_id:
                self.send_json([])
                return
            conn = get_db()
            cur = conn.cursor()
            cur.execute("SELECT * FROM CustomerPayments WHERE CustomerId = ? ORDER BY PaymentDate DESC", (cust_id,))
            rows = [dict(r) for r in cur.fetchall()]
            conn.close()
            self.send_json(rows)
            return

        if parsed.path == "/api/sales/history":
            conn = get_db()
            cur = conn.cursor()
            q = params.get('q', [''])[0].strip()
            date_filter = params.get('date', ['all'])[0]

            sql = "SELECT * FROM Sales WHERE 1=1"
            query_params = []

            if date_filter == 'today':
                today = datetime.now().strftime("%Y-%m-%d")
                sql += " AND CreatedAt >= ? AND CreatedAt <= ?"
                query_params.extend([f"{today}T00:00:00", f"{today}T23:59:59"])
            elif date_filter == 'week':
                week_ago = (datetime.now() - timedelta(days=7)).strftime("%Y-%m-%d")
                sql += " AND CreatedAt >= ?"
                query_params.append(f"{week_ago}T00:00:00")

            if q:
                sql += " AND (InvoiceNumber LIKE ? OR CustomerName LIKE ?)"
                like_q = f"%{q}%"
                query_params.extend([like_q, like_q])

            sql += " ORDER BY Id DESC LIMIT 100"
            cur.execute(sql, query_params)
            sales = [dict(r) for r in cur.fetchall()]

            for s in sales:
                cur.execute("SELECT * FROM SaleItems WHERE SaleId = ?", (s["Id"],))
                s["Items"] = [dict(it) for it in cur.fetchall()]

            conn.close()
            self.send_json(sales)
            return

        if parsed.path == "/api/reports/daily":
            conn = get_db()
            cur = conn.cursor()
            today = datetime.now().strftime("%Y-%m-%d")
            start = f"{today}T00:00:00"
            end = f"{today}T23:59:59"

            cur.execute("""
                SELECT 
                    COUNT(*) as TotalCount,
                    COALESCE(SUM(TotalAmount), 0) as TotalSales,
                    COALESCE(SUM(PaidAmount), 0) as CashCollected,
                    COALESCE(SUM(DebtAmount), 0) as CreditGiven,
                    COALESCE(SUM(ProfitAmount), 0) as NetProfit
                FROM Sales WHERE CreatedAt >= ? AND CreatedAt <= ?
            """, (start, end))
            stat = dict(cur.fetchone())

            cur.execute("SELECT COALESCE(SUM(Amount), 0) as DebtsCollected FROM CustomerPayments WHERE PaymentDate >= ? AND PaymentDate <= ?", (start, end))
            stat["DebtsCollected"] = cur.fetchone()["DebtsCollected"]

            cur.execute("SELECT COUNT(*) as LowStockCount FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStockAlert")
            stat["LowStockCount"] = cur.fetchone()["LowStockCount"]

            cur.execute("SELECT COALESCE(SUM(CurrentDebt), 0) as TotalGlobalDebt FROM Customers")
            stat["TotalGlobalDebt"] = cur.fetchone()["TotalGlobalDebt"]

            conn.close()
            self.send_json(stat)
            return

        if parsed.path == "/api/settings":
            conn = get_db()
            cur = conn.cursor()
            cur.execute("SELECT * FROM StoreSettings WHERE Id = 1")
            row = cur.fetchone()
            conn.close()
            self.send_json(dict(row) if row else {})
            return

        self.send_error(404, "Not Found")

    def do_POST(self):
        parsed = urllib.parse.urlparse(self.path)
        content_length = int(self.headers.get('Content-Length', 0))
        post_data = self.rfile.read(content_length).decode('utf-8')
        data = json.loads(post_data) if post_data else {}

        if parsed.path == "/api/activate":
            key = data.get("key", "").strip().upper()
            expected = generate_activation_key(MACHINE_ID)
            if key == expected:
                os.makedirs(os.path.dirname(LICENSE_FILE), exist_ok=True)
                with open(LICENSE_FILE, "w") as f:
                    f.write(f"{MACHINE_ID}\n{key}\nACTIVE\nACTIVE\nACTIVE\n3")
                self.send_json({"success": True, "message": "تم تفعيل البرنامج بنجاح مدى الحياة!"})
            else:
                self.send_json({"success": False, "message": "كود التفعيل غير مطابق لهذا الجهاز!"})
            return

        if parsed.path == "/api/sales":
            conn = get_db()
            cur = conn.cursor()
            try:
                cur.execute("SELECT COUNT(*) FROM Sales")
                count = cur.fetchone()[0] + 1
                inv_num = f"TICK-{datetime.now().year}-{count:05d}"

                subtotal = float(data.get("subtotal", 0))
                discount = float(data.get("discount", 0))
                total = float(data.get("total", subtotal - discount))
                paid = float(data.get("paid", total))
                change = float(data.get("change", 0))
                debt = float(data.get("debt", 0))
                profit = float(data.get("profit", 0))
                cust_id = data.get("customerId")
                cust_name = data.get("customerName", "زبون عادي (Comptoir)")
                items = data.get("items", [])
                created = datetime.now().isoformat()

                cur.execute("""
                    INSERT INTO Sales (InvoiceNumber, CustomerId, CustomerName, PaymentType, SubTotal, Discount, TotalAmount, PaidAmount, ChangeAmount, DebtAmount, ProfitAmount, CreatedAt)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (inv_num, cust_id, cust_name, 1 if debt > 0 else 0, subtotal, discount, total, paid, change, debt, profit, created))
                sale_id = cur.lastrowid

                for it in items:
                    cur.execute("""
                        INSERT INTO SaleItems (SaleId, ProductId, ProductReference, ProductName, Quantity, UnitPrice, PurchasePrice, TotalPrice)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                    """, (sale_id, it["productId"], it["reference"], it["name"], it["quantity"], it["unitPrice"], it.get("purchasePrice", 0), it["totalPrice"]))

                    cur.execute("UPDATE Products SET StockQuantity = StockQuantity - ?, UpdatedAt = ? WHERE Id = ?",
                                (it["quantity"], created, it["productId"]))

                if cust_id and debt > 0:
                    cur.execute("UPDATE Customers SET CurrentDebt = CurrentDebt + ?, UpdatedAt = ? WHERE Id = ?",
                                (debt, created, cust_id))

                conn.commit()
                self.send_json({"success": True, "invoiceNumber": inv_num, "saleId": sale_id})
            except Exception as e:
                conn.rollback()
                self.send_json({"success": False, "error": str(e)})
            finally:
                conn.close()
            return

        if parsed.path == "/api/customers/payment":
            conn = get_db()
            cur = conn.cursor()
            try:
                cust_id = data["customerId"]
                amount = float(data["amount"])
                notes = data.get("notes", "")

                cur.execute("SELECT CurrentDebt FROM Customers WHERE Id = ?", (cust_id,))
                cur_debt = cur.fetchone()[0]
                rem_debt = max(0.0, cur_debt - amount)
                now = datetime.now().isoformat()

                cur.execute("""
                    INSERT INTO CustomerPayments (CustomerId, Amount, PreviousDebt, RemainingDebt, Notes, PaymentDate)
                    VALUES (?, ?, ?, ?, ?, ?)
                """, (cust_id, amount, cur_debt, rem_debt, notes, now))

                cur.execute("UPDATE Customers SET CurrentDebt = ?, UpdatedAt = ? WHERE Id = ?", (rem_debt, now, cust_id))
                conn.commit()
                self.send_json({"success": True, "remainingDebt": rem_debt})
            except Exception as e:
                conn.rollback()
                self.send_json({"success": False, "error": str(e)})
            finally:
                conn.close()
            return

        if parsed.path == "/api/products/save":
            conn = get_db()
            cur = conn.cursor()
            try:
                p_id = data.get("id")
                ref = data["reference"]
                barcode = data.get("barcode") or f"200{int(datetime.now().timestamp()) % 1000000000:09d}0"
                name = data["name"]
                name_ar = data.get("nameAr")
                dim = data.get("dimensions")
                cost = float(data.get("purchasePrice", 0))
                price = float(data.get("salePrice", 0))
                wholesale = float(data.get("wholesalePrice", 0))
                stock = float(data.get("stockQuantity", 0))
                alert = float(data.get("minStockAlert", 5))
                now = datetime.now().isoformat()

                if p_id:
                    cur.execute("""
                        UPDATE Products SET Reference=?, Barcode=?, Name=?, NameAr=?, Dimensions=?,
                                           PurchasePrice=?, SalePrice=?, WholesalePrice=?, StockQuantity=?, MinStockAlert=?, UpdatedAt=?
                        WHERE Id=?
                    """, (ref, barcode, name, name_ar, dim, cost, price, wholesale, stock, alert, now, p_id))
                else:
                    cur.execute("""
                        INSERT INTO Products (Reference, Barcode, Name, NameAr, CategoryId, Dimensions,
                                             PurchasePrice, SalePrice, WholesalePrice, StockQuantity, MinStockAlert, CreatedAt, UpdatedAt)
                        VALUES (?, ?, ?, ?, 1, ?, ?, ?, ?, ?, ?, ?, ?)
                    """, (ref, barcode, name, name_ar, dim, cost, price, wholesale, stock, alert, now, now))

                conn.commit()
                self.send_json({"success": True})
            except Exception as e:
                conn.rollback()
                self.send_json({"success": False, "error": str(e)})
            finally:
                conn.close()
            return

        if parsed.path == "/api/products/delete":
            conn = get_db()
            cur = conn.cursor()
            try:
                p_id = data.get("id")
                cur.execute("UPDATE Products SET IsActive = 0, UpdatedAt = ? WHERE Id = ?", (datetime.now().isoformat(), p_id))
                conn.commit()
                self.send_json({"success": True})
            except Exception as e:
                conn.rollback()
                self.send_json({"success": False, "error": str(e)})
            finally:
                conn.close()
            return

        self.send_error(404, "Not Found")

    def send_json(self, data):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.end_headers()
        self.wfile.write(json.dumps(data, ensure_ascii=False).encode('utf-8'))

if __name__ == "__main__":
    os.chdir(BASE_DIR)
    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(("127.0.0.1", PORT), QLRequestHandler) as httpd:
        print(f"===============================================================")
        print(f"  QL System Test Server running on: http://localhost:{PORT}")
        print(f"===============================================================")
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nShutting down server.")
