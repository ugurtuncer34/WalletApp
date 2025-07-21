export interface TransactionReadDto {
    id: number,
    date: Date,
    amount: number,
    direction: string,
    accountId: number,
    accountName: string,
    categoryId: number,
    categoryName: string
}
