export interface TransactionUpdateDto {
    date: string; // or Date, but stringify before send
    amount: number;
    direction: number; // 1 or 2
    accountId: number;
    categoryId: number;
}
