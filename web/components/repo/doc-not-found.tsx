"use client";

import { FileQuestion } from "lucide-react";

interface DocNotFoundProps {
  slug: string;
}

export function DocNotFound({ slug }: DocNotFoundProps) {
  return (
    <div className="flex flex-col items-center justify-center py-20 px-4">
      <div className="rounded-full bg-muted/50 p-4 mb-6">
        <FileQuestion className="h-12 w-12 text-muted-foreground" />
      </div>
      <h2 className="text-xl font-semibold mb-2">文档不存在</h2>
      <p className="text-muted-foreground text-center max-w-md">
        未找到路径为 <code className="px-1.5 py-0.5 bg-muted rounded text-sm">{slug}</code> 的文档。
      </p>
      <p className="text-sm text-muted-foreground mt-4">
        请从左侧目录选择其他文档查看。
      </p>
    </div>
  );
}
