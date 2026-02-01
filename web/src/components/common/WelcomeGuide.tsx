import React, { useState, useEffect } from "react";
import { X, Map, ListTodo, FileBarChart } from "lucide-react";

interface WelcomeGuideProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function WelcomeGuide({ isOpen, onClose }: WelcomeGuideProps) {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setIsVisible(true);
    } else {
      setTimeout(() => setIsVisible(false), 300); // Wait for animation
    }
  }, [isOpen]);

  if (!isVisible && !isOpen) return null;

  return (
    <div
      className={`fixed inset-0 z-50 flex items-center justify-center p-4 transition-opacity duration-300 ${
        isOpen ? "opacity-100" : "opacity-0"
      }`}
    >
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Modal Content */}
      <div
        className={`relative w-full max-w-2xl bg-[#f9f8f5] rounded-[24px] shadow-2xl border border-white/50 overflow-hidden transform transition-all duration-300 ${
          isOpen ? "scale-100 translate-y-0" : "scale-95 translate-y-4"
        }`}
        dir="rtl"
      >
        {/* Header */}
        <div className="bg-gradient-to-r from-[#7895B2] to-[#647e99] p-6 text-white relative overflow-hidden">
          <div className="absolute top-0 left-0 w-32 h-32 bg-white/10 rounded-full -translate-x-10 -translate-y-10 blur-2xl" />
          <div className="absolute bottom-0 right-0 w-24 h-24 bg-black/5 rounded-full translate-x-10 translate-y-10 blur-xl" />

          <div className="relative z-10">
            <h2 className="text-2xl font-bold mb-2">
              مرحباً بك في نظام FollowUp
            </h2>
            <p className="text-white/90 text-sm">
              نظرة سريعة على خصائص النظام الأساسية
            </p>
          </div>

          <button
            onClick={onClose}
            className="absolute top-6 left-6 text-white/70 hover:text-white p-1 rounded-full hover:bg-white/10 transition-colors"
          >
            <X size={24} />
          </button>
        </div>

        {/* Body */}
        <div className="p-8 grid grid-cols-1 md:grid-cols-3 gap-6">
          <FeatureCard
            icon={<Map className="w-8 h-8 text-[#7895B2]" />}
            title="مراقبة ميدانية"
            description="تتبع مباشر لمواقع العمال والسيارات على الخريطة التفاعلية."
          />
          <FeatureCard
            icon={<ListTodo className="w-8 h-8 text-[#c97a63]" />}
            title="إدارة المهام"
            description="إسناد ومتابعة المهام الميدانية وحالة إنجازها لحظياً."
          />
          <FeatureCard
            icon={<FileBarChart className="w-8 h-8 text-[#a3b18a]" />}
            title="تقارير وإحصائيات"
            description="لوحة بيانات شاملة وتقارير مفصلة عن الأداء والإنجاز."
          />
        </div>

        {/* Footer */}
        <div className="p-6 bg-[#7895B2]/5 border-t border-[#7895B2]/10 flex justify-end">
          <button
            onClick={onClose}
            className="px-6 py-2.5 bg-[#7895B2] hover:bg-[#647e99] text-white font-medium rounded-xl shadow-lg shadow-[#7895B2]/20 transition-all active:scale-95"
          >
            بدء الاستخدام
          </button>
        </div>
      </div>
    </div>
  );
}

function FeatureCard({
  icon,
  title,
  description,
}: {
  icon: React.ReactNode;
  title: string;
  description: string;
}) {
  return (
    <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm hover:shadow-md hover:-translate-y-1 transition-all duration-300">
      <div className="w-14 h-14 rounded-full bg-gray-50 flex items-center justify-center mb-4">
        {icon}
      </div>
      <h3 className="font-bold text-[#2f2f2f] mb-2">{title}</h3>
      <p className="text-gray-500 text-sm leading-relaxed">{description}</p>
    </div>
  );
}
