export interface TransactionCreateDto {
    date: string,
    amount: number,
    direction: number,
    accountId: number,
    categoryId: number,
}
