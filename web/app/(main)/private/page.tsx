"use client";

import { useState, useCallback } from "react";
import { AppLayout } from "@/components/app-layout";
import { useTranslations } from "@/hooks/use-translations";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { RepositorySubmitForm } from "@/components/repo/repository-submit-form";
import { RepositoryList } from "@/components/repo/repository-list";
import {
  Sheet,
  SheetContent,
  SheetTrigger,
} from "@/components/animate-ui/components/radix/sheet";
import { useAuth } from "@/contexts/auth-context";

export default function PrivatePage() {
  const t = useTranslations();
  const { user } = useAuth();
  const [activeItem, setActiveItem] = useState(t("sidebar.private"));
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  const handleSubmitSuccess = useCallback(() => {
    setIsFormOpen(false);
    setRefreshTrigger((prev) => prev + 1);
  }, []);

  // Use a placeholder user ID if not authenticated
  const ownerUserId = user?.id ?? "anonymous";

  return (
    <AppLayout activeItem={activeItem} onItemClick={setActiveItem}>
      <div className="flex flex-1 flex-col gap-4 p-4 md:p-6">
        <div className="flex items-center justify-between">
          <div className="space-y-2">
            <h1 className="text-3xl font-bold tracking-tight">{t("sidebar.private")}</h1>
            <p className="text-muted-foreground">
              {t("common.privateRepos.description")}
            </p>
          </div>
          <Sheet open={isFormOpen} onOpenChange={setIsFormOpen}>
            <SheetTrigger asChild>
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                {t("home.addPrivateRepo")}
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
              <div className="pt-6">
                <RepositorySubmitForm
                  onSuccess={handleSubmitSuccess}
                />
              </div>
            </SheetContent>
          </Sheet>
        </div>

        <RepositoryList ownerId={ownerUserId} refreshTrigger={refreshTrigger} />
      </div>
    </AppLayout>
  );
}
