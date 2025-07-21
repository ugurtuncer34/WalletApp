export interface TransactionCreateDto {
    date: Date,
    amount: number,
    direction: number,
    accountId: number,
    categoryId: number,
}
