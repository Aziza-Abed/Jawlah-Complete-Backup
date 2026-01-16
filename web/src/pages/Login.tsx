import Button from "../components/ui/Button";

export default function Login() {
  return (
    <div className="min-h-screen w-full bg-[#D5D5D5] flex items-center justify-center">
      <div className="w-[520px] bg-white rounded-[16px] p-8 text-right">
        <h1 className="text-[36px] font-bold">تسجيل الدخول</h1>
        <p className="mt-2 text-black/60">نسخة تجريبية للربط لاحقًا مع الباك.</p>

        <div className="mt-8">
          <Button
            className="w-full rounded-[10px] h-[50px]"
            onClick={() => {
              localStorage.setItem("token", "demo");
              window.location.href = "/dashboard";
            }}
          >
            دخول (Demo)
          </Button>
        </div>
      </div>
    </div>
  );
}
