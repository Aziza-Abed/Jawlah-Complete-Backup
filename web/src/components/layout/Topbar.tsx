import React from "react";
import logo from "../../assets/logo.png";
import { Bell, User, Settings, Search } from "lucide-react";

export default function Topbar() {
  return (
    <header className="w-full h-[100px] bg-[#7895B2] px-8">
      <div className="h-full flex items-center justify-between">
        {/* RIGHT: Logo */}
        <div className="flex items-center justify-end w-[140px] mt-4">
          <img
            src={logo}
            alt="بلدية البيرة"
            className="w-[77px] h-[76px] object-contain"
          />
        </div>

        {/* CENTER: Search */}
        <div className="flex-1 flex justify-center">
          <div className="w-full max-w-[590px] h-[50px] bg-[#F3F1ED] rounded-full flex items-center px-5 gap-3">
            <Search size={20} className="text-[#60778E]" />
            <input
              type="text"
              placeholder="بحث..."
              className="w-full bg-transparent border-0 outline-none focus:outline-none focus:ring-0 text-right text-[16px] placeholder:text-[#60778E]/70"
            />
          </div>
        </div>

<div className="w-[200px] flex justify-start mt-2">
  <div className="h-[50px] bg-[#F3F1ED] rounded-full px-2 flex items-center gap-2">
    <PillIcon>
      <Bell size={22} className="text-[#60778E]" />
    </PillIcon>

    <div className="w-[1px] h-6 bg-[#60778E]/20" />

    <PillIcon>
      <User size={22} className="text-[#60778E]" />
    </PillIcon>

    <div className="w-[1px] h-6 bg-[#60778E]/20" />

    <PillIcon>
      <Settings size={22} className="text-[#60778E]" />
    </PillIcon>
  </div>
</div>


      </div>
    </header>
  );
}

function PillIcon({ children }: { children: React.ReactNode }) {
  return (
    <button
      type="button"
      className="
        w-[44px] h-[44px]
        rounded-full
        grid place-items-center
        bg-transparent
        border-0
        outline-none
        ring-0
        shadow-none
        focus:outline-none
        focus:ring-0
        hover:bg-white/70
        transition
      "
    >
      {children}
    </button>
  );
}
