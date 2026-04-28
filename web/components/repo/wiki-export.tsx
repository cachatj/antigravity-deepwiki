"use client";

import React, { useState } from "react";
import { Download, Loader2, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useTranslations } from "@/hooks/use-translations";
import { toast } from "sonner";
import { buildApiUrl } from "@/lib/api-client";
import { getToken } from "@/lib/auth-api";

interface WikiExportProps {
  owner: string;
  repo: string;
  currentBranch?: string;
  currentLanguage?: string;
  className?: string;
}

export function WikiExport({ 
  owner, 
  repo, 
  currentBranch, 
  currentLanguage,
  className 
}: WikiExportProps) {
  const [isExporting, setIsExporting] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const t = useTranslations();

  const handleExport = async () => {
    if (isExporting) return;
    
    setIsExporting(true);
    setIsSuccess(false);
    
    try {
      const params = new URLSearchParams();
      if (currentBranch) params.set("branch", currentBranch);
      if (currentLanguage) params.set("lang", currentLanguage);
      
      const endpoint = `/api/v1/repos/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/export${params.toString() ? `?${params.toString()}` : ""}`;
      const exportUrl = buildApiUrl(endpoint);
      
      const token = getToken();
      const headers: HeadersInit = {};
      if (token) {
        headers["Authorization"] = `Bearer ${token}`;
      }

      const response = await fetch(exportUrl, { headers });
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || "Export failed");
      }
      
      // Get filename from header or fallback
      const contentDisposition = response.headers.get("content-disposition");
      let fileName = `${owner}-${repo}-${currentBranch || "main"}-${currentLanguage || "en"}.zip`;
      if (contentDisposition) {
        const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (fileNameMatch?.[1]) {
          fileName = fileNameMatch[1].replace(/['"]/g, "");
        }
      }
      
      // Download process
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      
      setIsSuccess(true);
      toast.success(t("export.success") || "Documentation exported successfully");
      
      // Reset success state after a while
      setTimeout(() => setIsSuccess(false), 3000);
    } catch (error) {
      console.error("Export failed:", error);
      toast.error(t("export.failed") || "Failed at exporting documentation");
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <Button
      onClick={handleExport}
      disabled={isExporting}
      variant="outline"
      className={`relative group flex items-center gap-2 px-4 py-2 rounded-xl border-green-500/30 bg-green-500/5 text-green-700 dark:text-green-300 hover:bg-green-500/10 hover:border-green-500/50 transition-all font-medium overflow-hidden ${className}`}
    >
      {isExporting ? (
        <>
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>{t("export.exporting") || "Exporting..."}</span>
        </>
      ) : isSuccess ? (
        <>
          <CheckCircle2 className="h-4 w-4 text-green-500 animate-in zoom-in duration-300" />
          <span>{t("export.downloaded") || "Downloaded!"}</span>
        </>
      ) : (
        <>
          <Download className="h-4 w-4 group-hover:translate-y-0.5 transition-transform" />
          <span>{t("export.title") || "Export Wiki"}</span>
        </>
      )}
      
      {/* Subtle progress background for premium feel */}
      {isExporting && (
        <div className="absolute inset-x-0 bottom-0 h-0.5 bg-green-500/20">
          <div className="h-full bg-green-500 animate-progress origin-left w-full" />
        </div>
      )}
    </Button>
  );
}
